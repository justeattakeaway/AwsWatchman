using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancingV2;
using NUnit.Framework;
using Watchman.AwsResources.Services.Alb;

namespace Watchman.AwsResources.IntegrationTests.Service.Alb
{
    [TestFixture]
    public class AlbSourceTests
    {
        [Test]
        public async Task ReadAllFunctionsShouldReturnResults()
        {
            var source = InitializeAlbSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static AlbSource InitializeAlbSource()
        {
            var config = new AmazonElasticLoadBalancingV2Config
            {
                RegionEndpoint = RegionEndpoint.EUWest1
            };
            var albClient = new AmazonElasticLoadBalancingV2Client(config);

            return new AlbSource(albClient);
        }
    }
}
