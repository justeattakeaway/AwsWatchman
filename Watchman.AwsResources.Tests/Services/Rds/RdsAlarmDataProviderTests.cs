using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.RDS.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.Rds;

namespace Watchman.AwsResources.Tests.Services.Rds
{
    [TestFixture]
    public class RdsAlarmDataProviderTests
    {
        private DBInstance _dbInstance;
        private RdsAlarmDataProvider _rdsDataProvider;

        [TestFixtureSetUp]
        public void Setup()
        {
            _dbInstance = new DBInstance
            {
                DBInstanceIdentifier = "DBInstance Name",
                AllocatedStorage = 42
            };

            _rdsDataProvider = new RdsAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange

            //act
            var result = _rdsDataProvider.GetDimensions(_dbInstance, new List<string> { "DBInstanceIdentifier" });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_dbInstance.DBInstanceIdentifier));
            Assert.That(dim.Name, Is.EqualTo("DBInstanceIdentifier"));
        }

        [Test]
        [ExpectedException(UserMessage = "Unsupported dimension UnknownDimension")]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange

            //act
            var result = _rdsDataProvider.GetDimensions(_dbInstance, new List<string> { "UnknownDimension" });

            //assert
        }

        [Test]
        public void GetAttribute_KnownAttribute_ReturnsValue()
        {
            //arange

            //act
            var result = _rdsDataProvider.GetValue(_dbInstance, "AllocatedStorage");

            //assert
            var expected = _dbInstance.AllocatedStorage * (long) Math.Pow(10, 9);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(UserMessage = "Unsupported RDS property name")]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act
            var result = _rdsDataProvider.GetValue(_dbInstance, "Unknown Attribute");

            //assert
        }
    }
}
