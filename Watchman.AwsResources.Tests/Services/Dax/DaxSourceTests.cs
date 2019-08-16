using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DAX;
using Amazon.DAX.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Dax;

namespace Watchman.AwsResources.Tests.Services.Dax
{
    [TestFixture]
    public class DaxSourceTests
    {
        private DaxSource _source;
        private DescribeClustersResponse _firstPage;
        private DescribeClustersResponse _secondPage;
        private DescribeClustersResponse _thirdPage;

        [SetUp]
        public void SetUp()
        {
            var stepClusterMock = new Mock<IAmazonDAX>();

            _firstPage = new DescribeClustersResponse
            {
                NextToken = "token-1",
                Clusters = new List<Cluster>
                {
                    new Cluster
                    {
                        ClusterName = "ClusterName - 1"
                    }
                }
            };

            _secondPage = new DescribeClustersResponse
            {
                NextToken = "token-2",
                Clusters = new List<Cluster>
                {
                    new Cluster
                    {
                        ClusterName = "ClusterName - 2"
                    }
                }
            };


            _thirdPage = new DescribeClustersResponse
            {
                Clusters = new List<Cluster>
                {
                    new Cluster
                    {
                        ClusterName = "ClusterName - 3"
                    }
                }
            };

            stepClusterMock.Setup(c => c.DescribeClustersAsync(
                It.Is<DescribeClustersRequest>(r => r.NextToken == null)
                , It.IsAny<CancellationToken>()))
                .ReturnsAsync(_firstPage);

            stepClusterMock.Setup(c => c.DescribeClustersAsync(
                    It.Is<DescribeClustersRequest>(r => r.NextToken == "token-1")
                    , It.IsAny<CancellationToken>()))
                .ReturnsAsync(_secondPage);

            stepClusterMock.Setup(c => c.DescribeClustersAsync(
                    It.Is<DescribeClustersRequest>(r => r.NextToken == "token-2")
                    , It.IsAny<CancellationToken>()))
                .ReturnsAsync(_thirdPage);

            _source = new DaxSource(stepClusterMock.Object);
        }

        [Test]
        public async Task GetResourceNamesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // act
            var result = await _source.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.Clusters.Single().ClusterName));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.Clusters.Single().ClusterName));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.Clusters.Single().ClusterName));
        }

        [Test]
        public async Task GetResourceNamesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.NextToken = null;

            // act
            var result = await _source.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.Clusters.Single().ClusterName));
        }

        [Test]
        public async Task GetResourceNamesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.NextToken = null;
            _firstPage.Clusters = new List<Cluster>();

            // act
            var result = await _source.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResourceAsync_ReturnsCorrectResource()
        {
            // arrange
            var clusterName = _secondPage.Clusters.Single().ClusterName;

            // act
            var result = await _source.GetResourceAsync(clusterName);

            // assert
            Assert.That(result.Name, Is.EqualTo(clusterName));
            Assert.That(result.Resource, Is.InstanceOf<Cluster>());
            Assert.That(result.Resource.ClusterName, Is.EqualTo(clusterName));
        }
    }
}
