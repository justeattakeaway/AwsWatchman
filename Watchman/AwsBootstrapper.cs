using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ElasticLoadBalancing;
using Amazon.Lambda;
using Amazon.RDS;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.StepFunctions;
using StructureMap;
using Watchman.Engine;

namespace Watchman
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
            registry.For<IAmazonSimpleNotificationService>()
                .Use(ctx => new AmazonSimpleNotificationServiceClient(creds, new AmazonSimpleNotificationServiceConfig { RegionEndpoint = region }));
            registry.For<IAmazonRDS>()
                 .Use(ctx => new AmazonRDSClient(creds, new AmazonRDSConfig { RegionEndpoint = region }));
            registry.For<IAmazonAutoScaling>()
                 .Use(ctx => new AmazonAutoScalingClient(creds, region));
            registry.For<IAmazonCloudFormation>()
                .Use(ctx => new AmazonCloudFormationClient(creds, region));
            registry.For<IAmazonLambda>().Use(ctx => new AmazonLambdaClient(creds, region));
            registry.For<IAmazonEC2>().Use(ctx => new AmazonEC2Client(creds, region));
            registry.For<IAmazonElasticLoadBalancing>().Use(ctx => new AmazonElasticLoadBalancingClient(creds, region));
            registry.For<IAmazonS3>().Use(ctx => new AmazonS3Client(creds, region));
            registry.For<IAmazonStepFunctions>().Use(ctx => new AmazonStepFunctionsClient(creds, region));
            registry.For<IAmazonCloudWatch>().Use(ctx => new AmazonCloudWatchClient(creds, region));
        }
    }
}
