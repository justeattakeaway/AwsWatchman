using Amazon;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.DynamoDb;

namespace Watchman.AwsResources.IntegrationTests.Service.DynamoDb
{
    [TestFixture]
    public class TableDescriptionSourceTests
    {
        [Test]
        public async Task ReadAllTablesShouldReturnResults()
        {
            var source = InitializeTableSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static TableDescriptionSource InitializeTableSource()
        {
            var dbClient = new AmazonDynamoDBClient(CredentialsReader.GetCredentials(), RegionEndpoint.EUWest1);
            return new TableDescriptionSource(dbClient);
        }
    }
}
