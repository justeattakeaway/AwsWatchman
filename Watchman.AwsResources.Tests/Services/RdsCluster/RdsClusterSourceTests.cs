using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.RDS;
using Amazon.RDS.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.RdsCluster;

namespace Watchman.AwsResources.Tests.Services.RdsCluster
{
    [TestFixture]
    public class RdsClusterSourceTests
    {
        private DescribeDBClustersResponse _firstPage;
        private DescribeDBClustersResponse _secondPage;
        private DescribeDBClustersResponse _thirdPage;

        private RdsClusterSource _rdsClusterSource;

        [SetUp]
        public void Setup()
        {
            _firstPage = new DescribeDBClustersResponse
            {
                Marker = "token-1",
                DBClusters = new List<DBCluster>
                {
                    new DBCluster {DBClusterIdentifier = "DBCluster-1"}
                }
            };
            _secondPage = new DescribeDBClustersResponse
            {
                Marker = "token-2",
                DBClusters = new List<DBCluster>
                {
                    new DBCluster {DBClusterIdentifier = "DBCluster-2"}
                }
            };
            _thirdPage = new DescribeDBClustersResponse
            {
                DBClusters = new List<DBCluster>
                {
                    new DBCluster {DBClusterIdentifier = "DBCluster-3"}
                }
            };

            var rdsMock = new Mock<IAmazonRDS>();
            rdsMock.Setup(s => s.DescribeDBClustersAsync(
                It.Is<DescribeDBClustersRequest>(r => r.Marker == null),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_firstPage);

            rdsMock.Setup(s => s.DescribeDBClustersAsync(
                It.Is<DescribeDBClustersRequest>(r => r.Marker == "token-1"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_secondPage);

            rdsMock.Setup(s => s.DescribeDBClustersAsync(
                It.Is<DescribeDBClustersRequest>(r => r.Marker == "token-2"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_thirdPage);

            _rdsClusterSource = new RdsClusterSource(rdsMock.Object);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _rdsClusterSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.DBClusters.Single().DBClusterIdentifier));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.DBClusters.Single().DBClusterIdentifier));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.DBClusters.Single().DBClusterIdentifier));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.Marker = null;

            // act
            var result = await _rdsClusterSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.DBClusters.Single().DBClusterIdentifier));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.Marker = null;
            _firstPage.DBClusters = new List<DBCluster>();

            // act
            var result = await _rdsClusterSource.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondDBClusterName = _secondPage.DBClusters.First().DBClusterIdentifier;

            // act
            var result = await _rdsClusterSource.GetResourceAsync(secondDBClusterName);

            // assert
            Assert.That(result, Is.InstanceOf<DBCluster>());
            Assert.That(result.DBClusterIdentifier, Is.EqualTo(secondDBClusterName));
        }
    }
}
