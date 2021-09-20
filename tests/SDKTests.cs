using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using irsdkSharp.Enums;
using irsdkSharp.Models;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Session;
using NUnit.Framework;

namespace irsdkSharp.Tests
{
    public class Tests
    {
        IRacingSDK sdk;

        [OneTimeSetUp]
        public void Setup()
        {
            var memMap = MemoryMappedFile.CreateFromFile(Path.Combine("testdata", "session.ibt"));
            sdk = new IRacingSDK(accessor: memMap.CreateViewAccessor());
            Assert.IsTrue(sdk.Startup(false));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            sdk.Shutdown();
        }

        [Test]
        public void GetSerializedSession()
        {
            var session = sdk.GetSerializedSessionInfo();
            Assert.NotNull(session);
        }

        [Test]
        public void GetDataSerializedSession()
        {
            var session = sdk.GetData().Session;
            Assert.NotNull(session);
        }

        [Test]
        public void GetSerializedData()
        {
            var data = sdk.GetSerializedData();
            Assert.NotNull(data);
        }

        [Test]
        public void GetSessionInfo()
        {
            var session = sdk.GetSessionInfo();
            Assert.That(session, Does.Contain("WeekendInfo"));
        }

        [Test]
        public void GetDataProperty()
        {
            var data = sdk.GetSerializedData();
            Assert.NotZero(data.Data.SessionTick);
        }
        
        [Test]
        public void GetData()
        {
            var data = sdk.GetData();
            TestContext.WriteLine(data.ToString());
        }

        [Test]
        public void GetPositions()
        {
            var data = sdk.GetData();
            var positions = sdk.GetPositions(data);
            Assert.IsNotEmpty(positions);
        }
    }
}