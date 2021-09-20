using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using Data = irsdkSharp.Serialization.Models.Fastest.Data;

namespace irsdkSharp.Benchmark
{
    [MemoryDiagnoser]
    public class Runner : IDisposable
    {
        private readonly IRacingSDK sdk;
        private readonly IRacingDataModel _dataModel;
        private readonly Data _data;

        public Runner()
        {
            var memMap = MemoryMappedFile.CreateFromFile(Path.Combine("data", "session.ibt"));
            sdk = new IRacingSDK(accessor: memMap.CreateViewAccessor());
            sdk.Startup(false);

            _dataModel = sdk.GetSerializedData();
            _data = sdk.GetData();
        }

        // [Benchmark]
        public IRacingSessionModel SerializeSessionInformation() => sdk.GetSerializedSessionInfo();

        [Benchmark]
        public IRacingSessionModel DataSession() => _data.Session;

        // [Benchmark]
        public IRacingDataModel SerializeDataModel() => sdk.GetSerializedData();


        [Benchmark]
        public void IRacingDataModel_GetFloatValue()
        {
            var value = _dataModel.Data.AirPressure;
        }

        [Benchmark]
        public void IRacingDataModel_GetIntValue()
        {
            var value = _dataModel.Data.Gear;
        }

        [Benchmark]
        public void IRacingDataModel_GetBoolValue()
        {
            var value = _dataModel.Data.SteeringWheelUseLinear;
        }

        [Benchmark]
        public void IRacingDataModel_GetArrayValue()
        {
            var value = _dataModel.Data.LongAccel_ST;
        }

        [Benchmark]
        public Data Data() => sdk.GetData();

        [Benchmark]
        public void Data_GetFloatValue()
        {
            var value = _data.AirPressure;
        }

        [Benchmark]
        public void Data_GetIntValue()
        {
            var value = _data.Gear;
        }

        [Benchmark]
        public void Data_GetBoolValue()
        {
            var value = _data.dcStarter;
        }

        [Benchmark]
        public void Data_GetArrayValue()
        {
            var value = _data.CarIdxSteer;
        }

        [Benchmark]
        public void GetPositions() => sdk.GetPositions(_data);

        public void Dispose()
        {
            sdk.Shutdown();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Runner>();
        }
    }
}