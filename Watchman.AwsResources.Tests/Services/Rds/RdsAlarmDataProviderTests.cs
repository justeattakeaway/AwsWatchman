using Amazon.CloudWatch.Model;
using Amazon.RDS.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Watchman.AwsResources.Services.Rds;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.Rds
{
    [TestFixture]
    public class RdsAlarmDataProviderTests
    {
        private DBInstance _dbInstance;
        private RdsAlarmDataProvider _rdsDataProvider;

        [SetUp]
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
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<List<Dimension>> testDelegate =
                () => _rdsDataProvider.GetDimensions(_dbInstance, new List<string> { "UnknownDimension" });

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported dimension UnknownDimension"));
        }

        [Test]
        public async Task GetAttribute_KnownAttribute_ReturnsValue()
        {
            //arange

            //act
            var result = await _rdsDataProvider.GetValue(_dbInstance, new ResourceConfig(), "AllocatedStorage");

            //assert
            var expected = _dbInstance.AllocatedStorage * (long) Math.Pow(10, 9);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<Task> testDelegate =
                () => _rdsDataProvider.GetValue(_dbInstance, new ResourceConfig(), "Unknown Attribute");

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported RDS property name"));
        }
    }
}
