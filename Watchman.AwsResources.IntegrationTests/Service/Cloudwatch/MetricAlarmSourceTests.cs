using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Cloudwatch;

namespace Watchman.AwsResources.IntegrationTests.Service.Cloudwatch
{
    [TestFixture]
    public class MetricAlarmSourceTests
    {
        [Test]
        public async Task ReadAllAlarmsShouldReturnResults()
        {
            var source = InitializeSource();

            var resources = await source.GetResourcesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static MetricAlarmSource InitializeSource()
        {
            var client = new AmazonCloudWatchClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);
            return new MetricAlarmSource(client);
        }
    }
}
