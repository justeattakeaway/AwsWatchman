using System.Threading.Tasks;
using Amazon;
using Amazon.RDS;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.RdsCluster;

namespace Watchman.AwsResources.IntegrationTests.Service.RdsCluster
{
    [TestFixture]
    public class RdsClusterSourceTests
    {
        [Test]
        public async Task ReadAllDBClustersShouldReturnResults()
        {
            var source = InitializeRdsClusterSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static RdsClusterSource InitializeRdsClusterSource()
        {
            var rdsClient = new AmazonRDSClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new RdsClusterSource(rdsClient);
        }
    }
}
