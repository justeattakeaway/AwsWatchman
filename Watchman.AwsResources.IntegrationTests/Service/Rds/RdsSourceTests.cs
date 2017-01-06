using System.Threading.Tasks;
using Amazon;
using NUnit.Framework;
using Amazon.RDS;
using TestHelper;
using Watchman.AwsResources.Services.Rds;

namespace Watchman.AwsResources.IntegrationTests.Service.Rds
{
    [TestFixture]
    public class RdsSourceTests
    {
        [Test]
        public async Task ReadAllDbInstancesShouldReturnResults()
        {
            var source = InitializeRdsSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static RdsSource InitializeRdsSource()
        {
            var lambdaClient = new AmazonRDSClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new RdsSource(lambdaClient);
        }
    }
}
