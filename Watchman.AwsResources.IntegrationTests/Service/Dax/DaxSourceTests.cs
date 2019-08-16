using System.Threading.Tasks;
using Amazon;
using Amazon.DAX;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Dax;

namespace Watchman.AwsResources.IntegrationTests.Service.Dax
{
    public class DaxSourceTests
    {
        [Test]
        public async Task GetResourceNamesAsyncShouldReturnResults()
        {
            var source = InitializeSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static DaxSource InitializeSource()
        {
            var amazonDaxClient = new AmazonDAXClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);
            return new DaxSource(amazonDaxClient);
        }
    }
}
