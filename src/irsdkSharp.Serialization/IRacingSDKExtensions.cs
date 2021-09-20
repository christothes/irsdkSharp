﻿using irsdkSharp.Models;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using System.Collections.Generic;
using System.Diagnostics;
using Data = irsdkSharp.Serialization.Models.Fastest.Data;

namespace irsdkSharp.Serialization
{
    public static class IRacingSDKExtensions
    {
        public static Data GetData(this IRacingSDK racingSDK) 
            => new Data(racingSDK);

        public static IRacingSessionModel GetSerializedSessionInfo(this IRacingSDK racingSDK)
        {
            var sessionInfo = racingSDK.GetSessionInfo();

            if (sessionInfo == null)
            {
                return null;
            }

            return IRacingSessionModel.Serialize(sessionInfo);
        }

        public static IRacingDataModel GetSerializedData(this IRacingSDK racingSDK)
        {
            if (racingSDK.IsInitialized && racingSDK.Header != null)
            {
                var fileView = IRacingSDK.GetFileMapView(racingSDK);
                var headers = IRacingSDK.GetVarHeaders(racingSDK);
                var data = new byte[racingSDK.Header.BufferLength];
                fileView.ReadArray(racingSDK.Header.Offset, data, 0, racingSDK.Header.BufferLength);
                return IRacingDataModel.Serialize(data, headers);
            }
            return null;
        }

        public static List<PositionModel> GetPositions(this IRacingSDK sdk, Data data)
        {
            var tick = data.SessionTick;
            var CarIdxBestLapNum = data.CarIdxBestLapNum;
            var CarIdxBestLapTime = data.CarIdxBestLapTime;
            var CarIdxClassPosition = data.CarIdxClassPosition;
            var CarIdxEstTime = data.CarIdxEstTime;
            var CarIdxF2Time = data.CarIdxF2Time;
            var CarIdxGear = data.CarIdxGear;
            var CarIdxLap = data.CarIdxLap;
            var CarIdxLapCompleted = data.CarIdxLapCompleted;
            var CarIdxLapDistPct = data.CarIdxLapDistPct;
            var CarIdxLastLapTime = data.CarIdxLastLapTime;
            var CarIdxOnPitRoad = data.CarIdxOnPitRoad;
            var CarIdxP2P_Count = data.CarIdxP2P_Count;
            var CarIdxP2P_Status = data.CarIdxP2P_Status;
            var CarIdxPosition = data.CarIdxPosition;
            var CarIdxRPM = data.CarIdxRPM;
            var CarIdxSteer = data.CarIdxSteer;
            var CarIdxTrackSurface = data.CarIdxTrackSurface;
            var CarIdxTrackSurfaceMaterial = data.CarIdxTrackSurfaceMaterial;

            var results = new List<PositionModel>(64);
            for (var i = 0; i< 64; i++)
            {
                results.Add(new PositionModel
                {
                    CarIdx = i,
                    CarIdxBestLapNum = CarIdxBestLapNum[i],
                    CarIdxBestLapTime = CarIdxBestLapTime[i],
                    CarIdxClassPosition = CarIdxClassPosition[i],
                    CarIdxEstTime = CarIdxEstTime[i],
                    CarIdxF2Time = CarIdxF2Time[i],
                    CarIdxGear = CarIdxGear[i],
                    CarIdxLap = CarIdxLap[i],
                    CarIdxLapCompleted = CarIdxLapCompleted[i],
                    CarIdxLapDistPct = CarIdxLapDistPct[i],
                    CarIdxLastLapTime = CarIdxLastLapTime[i],
                    CarIdxOnPitRoad = CarIdxOnPitRoad[i],
                    CarIdxP2P_Count = CarIdxP2P_Count[i],
                    CarIdxP2P_Status = CarIdxP2P_Status[i],
                    CarIdxPosition = CarIdxPosition[i],
                    CarIdxRPM = CarIdxRPM[i],
                    CarIdxSteer = CarIdxSteer[i],
                    CarIdxTrackSurface = CarIdxTrackSurface[i],
                    CarIdxTrackSurfaceMaterial = CarIdxTrackSurfaceMaterial[i],
                    SessionTime = tick
                });
            }
            return results;
        }
    }
}
