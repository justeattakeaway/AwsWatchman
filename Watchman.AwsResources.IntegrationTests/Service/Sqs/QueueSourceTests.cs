using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using Amazon.SQS;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Sqs;

namespace Watchman.AwsResources.IntegrationTests.Service.Sqs
{
    [TestFixture]
    public class QueueSourceTests
    {
        [Test]
        public async Task ReadAllQueuesShouldReturnResults()
        {
            var source = InitializeQueueSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static QueueSource InitializeQueueSource()
        {
            var creds = CredentialsReader.GetCredentials();
            var cloudWatchClient = new AmazonCloudWatchClient(creds, RegionEndpoint.EUWest1);
            var sqsClient = new AmazonSQSClient(creds, RegionEndpoint.EUWest1);
            return new QueueSource(cloudWatchClient, sqsClient);
        }
    }
}
