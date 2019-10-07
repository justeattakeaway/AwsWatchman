using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.RDS.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Watchman.AwsResources.Services.RdsCluster;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.RdsCluster
{
    [TestFixture]
    public class RdsClusterAlarmDataProviderTests
    {
        private DBCluster _dbCluster;
        private RdsClusterAlarmDataProvider _rdsClusterDataProvider;

        [SetUp]
        public void Setup()
        {
            _dbCluster = new DBCluster
            {
                AllocatedStorage = 4
            };

            _rdsClusterDataProvider = new RdsClusterAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange

            //act
            var result = _rdsClusterDataProvider.GetDimensions(_dbCluster, new List<string> {"DBClusterIdentifier"});

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_dbCluster.DBClusterIdentifier));
            Assert.That(dim.Name, Is.EqualTo("DBClusterIdentifier"));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<List<Dimension>> testDelegate =
                () => _rdsClusterDataProvider.GetDimensions(_dbCluster, new List<string> {"UnknownDimension"});

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported dimension UnknownDimension"));
        }

        [Test]
        public async Task GetAttribute_KnownAttribute_ReturnsValue()
        {
            //arange

            //act
            var result = await _rdsClusterDataProvider.GetValue(_dbCluster, new ResourceConfig(), "AllocatedStorage");

            //assert
            var expected = _dbCluster.AllocatedStorage * (long) Math.Pow(10, 9);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<Task> testDelegate =
                () => _rdsClusterDataProvider.GetValue(_dbCluster, new ResourceConfig(), "Unknown Attribute");

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported RDSCluster property name"));
        }
    }
}
