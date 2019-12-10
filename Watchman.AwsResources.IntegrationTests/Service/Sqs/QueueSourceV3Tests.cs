using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Sqs;
using Watchman.AwsResources.Services.Sqs.V3;

namespace Watchman.AwsResources.IntegrationTests.Service.Sqs
{
    [TestFixture]
    public class QueueSourceV3Tests
    {
        [Test]
        public async Task ReadAllQueuesShouldReturnResults()
        {
            var source = InitializeQueueSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static QueueDataSourceV3 InitializeQueueSource()
        {
            var creds = CredentialsReader.GetCredentials();
            var cloudWatchClient = new AmazonCloudWatchClient(creds, RegionEndpoint.EUWest1);
            return new QueueDataSourceV3(new QueueSource(cloudWatchClient));
        }
    }
}
