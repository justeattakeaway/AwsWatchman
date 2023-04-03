using Amazon;
using Amazon.AutoScaling;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.AutoScaling;

namespace Watchman.AwsResources.IntegrationTests.Service.AutoScaling
{
    [TestFixture]
    public class AutoScalingSourceTests
    {
        [Test]
        public async Task ReadAllDbInstancesShouldReturnResults()
        {
            var source = InitializeAutoScalingGroupSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static AutoScalingGroupSource InitializeAutoScalingGroupSource()
        {
            var lambdaClient = new AmazonAutoScalingClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new AutoScalingGroupSource(lambdaClient);
        }
    }
}
