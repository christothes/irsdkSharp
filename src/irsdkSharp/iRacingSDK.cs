using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using irsdkSharp.Enums;
using irsdkSharp.Models;
using System.Threading;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using irsdkSharp.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace irsdkSharp
{
    public class IRacingSDK
    {
        private readonly Encoding _encoding;
        private char[] trimChars = { '\0' };
        private readonly AutoResetEvent _gameLoopEvent;
        private readonly ILogger<IRacingSDK> _logger;
        private MemoryMappedViewAccessor _fileMapView;
        private Dictionary<string, VarHeader> _varHeaders;
        
        //VarHeader offsets
        private const int VarOffsetOffset = 4;
        private const int VarCountOffset = 8;
        private const int VarNameOffset = 16;
        private const int VarDescOffset = 48;
        private const int VarUnitOffset = 112;

        /// <summary>
        /// Indicates if the SDK is initialized and connected to the telemetry feed.
        /// </summary>
        public bool IsInitialized = false;

        public static MemoryMappedViewAccessor GetFileMapView(IRacingSDK racingSDK)
        {
            return racingSDK._fileMapView;
        }

        public static Dictionary<string, VarHeader> GetVarHeaders(IRacingSDK racingSDK)
        {
            return racingSDK._varHeaders;
        }

        public IRacingSdkHeader Header = null;

       

        public IRacingSDK(ILogger<IRacingSDK> logger = null) : this(null, logger)
        {
            
        }

        public IRacingSDK(MemoryMappedViewAccessor accessor, ILogger<IRacingSDK> logger = null)
        {
            _fileMapView = accessor;
            _logger = logger ?? NullLogger<IRacingSDK>.Instance;
            // Register CP1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encoding = Encoding.GetEncoding(1252);
        }

        public bool Startup(bool openWaitHandle = true)
        {
            if (IsInitialized) return true;

            try
            {
                if (openWaitHandle)
                {
                    var iRacingFile = MemoryMappedFile.OpenExisting(Constants.MemMapFileName);
                    _fileMapView = iRacingFile.CreateViewAccessor();
                    using var hEvent = EventWaitHandle.OpenExisting(Constants.DataValidEventName);
                    if (!hEvent.WaitOne(TimeSpan.FromSeconds(1)))
                    {
                        Console.WriteLine("Failed to wait on DataValid Event.");
                        return false;
                    }
                }

                Header = new IRacingSdkHeader(_fileMapView);
                GetVarHeaders();

                IsInitialized = true;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void GetVarHeaders()
        {
            _varHeaders = new Dictionary<string, VarHeader>(Header.VarCount);
            for (int i = 0; i < Header.VarCount; i++)
            {
                int type = _fileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeader.Size)));
                int offset = _fileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeader.Size) + VarOffsetOffset));
                int count = _fileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeader.Size) + VarCountOffset));
                byte[] name = new byte[Constants.MaxString];
                byte[] desc = new byte[Constants.MaxDesc];
                byte[] unit = new byte[Constants.MaxString];
                _fileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeader.Size) + VarNameOffset), name, 0, Constants.MaxString);
                _fileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeader.Size) + VarDescOffset), desc, 0, Constants.MaxDesc);
                _fileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeader.Size) + VarUnitOffset), unit, 0, Constants.MaxString);
                string nameStr = _encoding.GetString(name).TrimEnd(trimChars);
                string descStr = _encoding.GetString(desc).TrimEnd(trimChars);
                string unitStr = _encoding.GetString(unit).TrimEnd(trimChars);
                var header = new VarHeader(type, offset, count, nameStr, descStr, unitStr);
                _varHeaders[header.Name] = header;  
            }
        }

        public object GetData(string name)
        {
            if (!IsInitialized || Header == null) return null;
            if (!_varHeaders.TryGetValue(name, out var requestedHeader)) return null;

            int varOffset = requestedHeader.Offset;
            int count = requestedHeader.Count;

            switch (requestedHeader.Type)
            {
                case VarType.irChar:
                    {
                        byte[] data = new byte[count];
                        _fileMapView.ReadArray(Header.Offset + varOffset, data, 0, count);
                        return _encoding.GetString(data).TrimEnd(trimChars);
                    }
                case VarType.irBool:
                    {
                        if (count > 1)
                        {
                            bool[] data = new bool[count];
                            _fileMapView.ReadArray(Header.Offset + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return _fileMapView.ReadBoolean(Header.Offset + varOffset);
                        }
                    }
                case VarType.irInt:
                case VarType.irBitField:
                    {
                        if (count > 1)
                        {
                            int[] data = new int[count];
                            _fileMapView.ReadArray(Header.Offset + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return _fileMapView.ReadInt32(Header.Offset + varOffset);
                        }
                    }
                case VarType.irFloat:
                    {
                        if (count > 1)
                        {
                            float[] data = new float[count];
                            _fileMapView.ReadArray(Header.Offset + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return _fileMapView.ReadSingle(Header.Offset + varOffset);
                        }
                    }
                case VarType.irDouble:
                    {
                        if (count > 1)
                        {
                            double[] data = new double[count];
                            _fileMapView.ReadArray(Header.Offset + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return _fileMapView.ReadDouble(Header.Offset + varOffset);
                        }
                    }
                default: return null;
            }
        }

        public string GetSessionInfo() =>
            (IsInitialized && Header != null) switch
            {
                true => _fileMapView.ReadString(Header.SessionInfoOffset, Header.SessionInfoLength),
                _ => null
            };

        public bool IsConnected()
        {
            if (IsInitialized && Header != null)
            {
                return (Header.Status & 1) > 0;
            }
            return false;
        }

        public void Shutdown()
        {
            IsInitialized = false;
            Header = null;
            _fileMapView?.Dispose();
        }

        IntPtr GetBroadcastMessageID()
        {
            return RegisterWindowMessage(Constants.BroadcastMessageName);
        }

        IntPtr GetPadCarNumID()
        {
            return RegisterWindowMessage(Constants.PadCarNumName);
        }

        public int BroadcastMessage(BroadcastMessageTypes msg, int var1, int var2, int var3)
        {
            return BroadcastMessage(msg, var1, MakeLong((short)var2, (short)var3));
        }

        public int BroadcastMessage(BroadcastMessageTypes msg, int var1, int var2)
        {
            IntPtr msgId = GetBroadcastMessageID();
            IntPtr hwndBroadcast = IntPtr.Add(IntPtr.Zero, 0xffff);
            IntPtr result = IntPtr.Zero;
            if (msgId != IntPtr.Zero)
            {
                result = PostMessage(hwndBroadcast, msgId.ToInt32(), MakeLong((short)msg, (short)var1), var2);
            }
            return result.ToInt32();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr RegisterWindowMessage(string lpProcName);

        //[DllImport("user32.dll")]
        //private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr OpenEvent(UInt32 dwDesiredAccess, Boolean bInheritHandle, String lpName);

        public int MakeLong(short lowPart, short highPart)
        {
            return (int)(((ushort)lowPart) | (uint)(highPart << 16));
        }

        public static short HiWord(int dword)
        {
            return (short)(dword >> 16);
        }

        public static short LoWord(int dword)
        {
            return (short)dword;
        }
    }
}