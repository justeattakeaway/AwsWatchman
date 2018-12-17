using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancingV2;
using NUnit.Framework;
using TestHelper;
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
            var albClient = new AmazonElasticLoadBalancingV2Client(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);
            return new AlbSource(albClient);
        }
    }
}
