using Amazon;
using Amazon.Lambda;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Lambda;

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
