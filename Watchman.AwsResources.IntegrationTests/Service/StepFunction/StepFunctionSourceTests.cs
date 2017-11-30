using System.Threading.Tasks;
using Amazon;
using Amazon.StepFunctions;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.StepFunction;

namespace Watchman.AwsResources.IntegrationTests.Service.StepFunction
{
    public class StepFunctionSourceTests
    {
        [Test]
        public async Task GetResourceNamesAsyncShouldReturnResults()
        {
            var source = InitializeSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private StepFunctionSource InitializeSource()
        {
            var amazonStepFunctions = new AmazonStepFunctionsClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);
            return new StepFunctionSource(amazonStepFunctions);
        }
    }
}
