using System.Threading.Tasks;
using Amazon;
using NUnit.Framework;
using Watchman.AwsResources.Services.Lambda;
using Amazon.Lambda;
using TestHelper;

namespace Watchman.AwsResources.IntegrationTests.Service.Lambda
{
    [TestFixture]
    public class LambdaSourceTests
    {
        [Test]
        public async Task ReadAllFunctionsShouldReturnResults()
        {
            var source = InitializeLambdaSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static LambdaSource InitializeLambdaSource()
        {
            var lambdaClient = new AmazonLambdaClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new LambdaSource(lambdaClient);
        }
    }
}
