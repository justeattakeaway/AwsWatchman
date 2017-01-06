using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancing;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Elb;

namespace Watchman.AwsResources.IntegrationTests.Service.Elb
{
    [TestFixture]
    public class ElbSourceTests
    {
        [Test]
        public async Task ReadAllFunctionsShouldReturnResults()
        {
            var source = InitializeElbSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static ElbSource InitializeElbSource()
        {
            var elbClient = new AmazonElasticLoadBalancingClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new ElbSource(elbClient);
        }
    }
}