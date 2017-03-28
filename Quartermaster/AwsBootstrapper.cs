using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using StructureMap;
using Watchman.Engine;

namespace Quartermaster
{
    public static class AwsBootstrapper
    {
        public static void Configure(IProfileRegistry registry, StartupParameters parameters)
        {
            var region = AwsCredentialsHelper.ReadAwsRegion(parameters.AwsRegion);
            var creds = AwsCredentialsHelper.ReadAwsCredentials(
                parameters.AwsAccessKey, parameters.AwsSecretKey, parameters.AwsProfile);

            registry.For<IAmazonDynamoDB>()
                .Use(ctx => new AmazonDynamoDBClient(creds, new AmazonDynamoDBConfig { RegionEndpoint = region }));
            registry.For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds, new AmazonCloudWatchConfig { RegionEndpoint = region }));
        }
    }
}
