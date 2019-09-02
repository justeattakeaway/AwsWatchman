using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Alb;

namespace Watchman.AwsResources.Tests.Services.Alb
{
    [TestFixture]
    public class AlbSourceTests
    {
        private DescribeLoadBalancersResponse _firstPage;
        private DescribeLoadBalancersResponse _secondPage;
        private DescribeLoadBalancersResponse _thirdPage;
        private DescribeLoadBalancersResponse _fourthPage;

        private AlbSource _albSource;

        [SetUp]
        public void Setup()
        {
            _firstPage = new DescribeLoadBalancersResponse
            {
                NextMarker = "token-1",
                LoadBalancers = new List<LoadBalancer>
                {
                    new LoadBalancer {LoadBalancerName = "LoadBalancer-1", Type = LoadBalancerTypeEnum.Application}
                }
            };
            _secondPage = new DescribeLoadBalancersResponse
            {
                NextMarker = "token-2",
                LoadBalancers = new List<LoadBalancer>
                {
                    new LoadBalancer {LoadBalancerName = "LoadBalancer-2", Type = LoadBalancerTypeEnum.Application}
                }
            };
            _thirdPage = new DescribeLoadBalancersResponse
            {
                NextMarker = "token-3",
                LoadBalancers = new List<LoadBalancer>
                {
                    new LoadBalancer {LoadBalancerName = "LoadBalancer-3", Type = LoadBalancerTypeEnum.Application}
                }
            };
            _fourthPage = new DescribeLoadBalancersResponse
            {
                LoadBalancers = new List<LoadBalancer>
                {
                    new LoadBalancer {LoadBalancerName = "NetworkBalancer-1", Type = LoadBalancerTypeEnum.Network}
                }
            };

            var elbMock = new Mock<IAmazonElasticLoadBalancingV2>();
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

            elbMock.Setup(s => s.DescribeLoadBalancersAsync(
                It.Is<DescribeLoadBalancersRequest>(r => r.Marker == "token-3"),
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(_fourthPage);

            _albSource = new AlbSource(elbMock.Object);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _albSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.LoadBalancers.Single().LoadBalancerName));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.LoadBalancers.Single().LoadBalancerName));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.LoadBalancers.Single().LoadBalancerName));
        }

        [Test]
        public async Task GetResourcesAsync_OnlyApplicationBalancersReturned()
        {
            // arrange

            // act
            var result = await _albSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Where(x => x.Equals(_fourthPage.LoadBalancers.Single().LoadBalancerName,
                    StringComparison.InvariantCultureIgnoreCase)), Is.Empty);
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.NextMarker = null;

            // act
            var result = await _albSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.LoadBalancers.Single().LoadBalancerName));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.NextMarker = null;
            _firstPage.LoadBalancers = new List<LoadBalancer>();

            // act
            var result = await _albSource.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondFunctionName = _secondPage.LoadBalancers.First().LoadBalancerName;

            // act
            var result = await _albSource.GetResourceAsync(secondFunctionName);

            // assert
            Assert.That(result, Is.InstanceOf<AlbResource>());
            Assert.That(result.LoadBalancer.LoadBalancerName, Is.EqualTo(secondFunctionName));
        }
    }
}
