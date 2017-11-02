using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.Kinesis;

namespace Watchman.AwsResources.IntegrationTests.Service.Kinesis
{
    [TestFixture]
    public class KinesisStreamSourceTests
    {
        [Test]
        public async Task ReadAllStreamsShouldReturnResults()
        {
            var source = InitializeStreamSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static KinesisStreamSource InitializeStreamSource()
        {
            var cloudWatchClient = new AmazonCloudWatchClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);

            return new KinesisStreamSource(cloudWatchClient);
        }
    }
}
