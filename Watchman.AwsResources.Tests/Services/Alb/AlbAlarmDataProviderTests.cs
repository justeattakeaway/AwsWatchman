using Amazon.ElasticLoadBalancingV2.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.Alb;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.Alb
{
    [TestFixture]
    public class AlbAlarmDataProviderTests
    {
        private AlbAlarmDataProvider _albDataProvider;

        [SetUp]
        public void Setup()
        {
            _albDataProvider = new AlbAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arrange
            var expectedDimensionValue = "name-of-load-balancer-12V7PE8E9TI1P/857b27423713d35c";
            var albResource = new AlbResource
            {
                LoadBalancer = new LoadBalancer { 
                    LoadBalancerArn = $"loadbalancer/{expectedDimensionValue}"
                }
            };
            const string dimName = "LoadBalancer";

            //act
            var result = _albDataProvider.GetDimensions(albResource, new List<string> { dimName });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(expectedDimensionValue));
            Assert.That(dim.Name, Is.EqualTo(dimName));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arrange
            var albResource = new AlbResource
            {
                LoadBalancer = new LoadBalancer
                {
                    LoadBalancerArn = "loadbalancer/name-of-load-balancer"
                }
            };
            const string dimName = "UnknownDimension";

            //act

            //assert
            var ex = Assert.Throws<Exception>(() => _albDataProvider.GetDimensions(albResource, new List<string> { dimName }));
            Assert.That(ex.Message, Is.EqualTo($"Unsupported dimension {dimName}"));
        }

        [Test]
        public void GetDimensions_NotMatchingValue_ThrowException()
        {
            //arrange
            var albResource = new AlbResource
            {
                LoadBalancer = new LoadBalancer
                {
                    LoadBalancerArn = "not-matching/load-balancer-name"
                }
            };
            const string dimName = "LoadBalancer";

            //act

            //assert
            var ex = Assert.Throws<Exception>(() => _albDataProvider.GetDimensions(albResource, new List<string> { dimName }));
            Assert.That(ex.Message, Is.EqualTo($"Could not find dimension value for LoadBalancer '{albResource.LoadBalancer.LoadBalancerArn}'"));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arrange
            var albResource = new AlbResource
            {
                LoadBalancer = new LoadBalancer
                {
                    LoadBalancerArn = "loadbalancer/name-of-load-balancer"
                }
            };

            //act

            //assert
            Assert.Throws<NotImplementedException>(() => _albDataProvider.GetValue(albResource, new ResourceConfig(), "SomeAttribute"));
        }
    }
}
