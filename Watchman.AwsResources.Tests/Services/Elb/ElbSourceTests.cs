using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Elb;

namespace Watchman.AwsResources.Tests.Services.Elb
{
    [TestFixture]
    public class ElbSourceTests
    {
        private DescribeLoadBalancersResponse _firstPage;
        private DescribeLoadBalancersResponse _secondPage;
        private DescribeLoadBalancersResponse _thirdPage;

        private ElbSource _elbSource;

        [SetUp]
        public void Setup()
        {
            _firstPage = new DescribeLoadBalancersResponse
            {
                NextMarker = "token-1",
                LoadBalancerDescriptions = new List<LoadBalancerDescription>
                {
                    new LoadBalancerDescription {LoadBalancerName = "LoadBalancer-1"}
                }
            };
            _secondPage = new DescribeLoadBalancersResponse
            {
                NextMarker = "token-2",
                LoadBalancerDescriptions = new List<LoadBalancerDescription>
                {
                    new LoadBalancerDescription {LoadBalancerName = "LoadBalancer-2"}
                }
            };
            _thirdPage = new DescribeLoadBalancersResponse
            {
                LoadBalancerDescriptions = new List<LoadBalancerDescription>
                {
                    new LoadBalancerDescription {LoadBalancerName = "LoadBalancer-3"}
                }
            };

            var elbMock = new Mock<IAmazonElasticLoadBalancing>();
            elbMock.Setup(s => s.DescribeLoadBalancersAsync(
                It.Is<DescribeLoadBalancersRequest>(r => r.Marker == null),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_firstPage);

            elbMock.Setup(s => s.DescribeLoadBalancersAsync(
                It.Is<DescribeLoadBalancersRequest>(r => r.Marker == "token-1"),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_secondPage);

            elbMock.Setup(s => s.DescribeLoadBalancersAsync(
                It.Is<DescribeLoadBalancersRequest>(r => r.Marker == "token-2"),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_thirdPage);

            _elbSource = new ElbSource(elbMock.Object);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _elbSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.LoadBalancerDescriptions.Single().LoadBalancerName));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.LoadBalancerDescriptions.Single().LoadBalancerName));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.LoadBalancerDescriptions.Single().LoadBalancerName));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.NextMarker = null;

            // act
            var result = await _elbSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.LoadBalancerDescriptions.Single().LoadBalancerName));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.NextMarker = null;
            _firstPage.LoadBalancerDescriptions = new List<LoadBalancerDescription>();

            // act
            var result = await _elbSource.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondFunctionName = _secondPage.LoadBalancerDescriptions.First().LoadBalancerName;

            // act
            var result = await _elbSource.GetResourceAsync(secondFunctionName);

            // assert
            Assert.That(result.Name, Is.EqualTo(secondFunctionName));
            Assert.That(result.Resource, Is.InstanceOf<LoadBalancerDescription>());
            Assert.That(result.Resource.LoadBalancerName, Is.EqualTo(secondFunctionName));
        }
    }
}
