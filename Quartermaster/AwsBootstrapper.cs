using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using StructureMap;
using Watchman.Engine;

namespace Quartermaster
{
    public static class AwsBootstrapper
    {
        public static void Configure(IProfileRegistry registry, StartupParameters parameters)
        {
            var region = AwsStartup.ParseRegion(parameters.AwsRegion);
            var creds = AwsStartup.CredentialsWithFallback(
                parameters.AwsAccessKey, parameters.AwsSecretKey, parameters.AwsProfile);

            registry.For<IAmazonDynamoDB>()
                .Use(ctx => new AmazonDynamoDBClient(creds, new AmazonDynamoDBConfig { RegionEndpoint = region }));
            registry.For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds, new AmazonCloudWatchConfig { RegionEndpoint = region }));
        }
    }
}
