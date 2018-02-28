using Amazon;
using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ElasticLoadBalancing;
using Amazon.Lambda;
using Amazon.RDS;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.StepFunctions;
using StructureMap;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine;
using Watchman.Engine.Logging;

namespace Watchman.IoC
{
    public class BoundaryRegistry : Registry
    {
        public BoundaryRegistry(StartupParameters parameters)
        {
            For<IConfigLoader>().Use<ConfigLoader>();
            For<ICurrentTimeProvider>().Use<CurrentTimeProvider>();

            ConfigureLogging(parameters);
            ConfigureAwsClients(parameters);
        }

        private void ConfigureLogging(StartupParameters parameters)
        {
            var alarmLogger = new ConsoleAlarmLogger(parameters.Verbose);
            var loadLogger = new ConsoleConfigLoadLogger(parameters.Verbose);

            For<IAlarmLogger>().Use(alarmLogger);
            For<IConfigLoadLogger>().Use(loadLogger);
        }

        private void ConfigureAwsClients(StartupParameters parameters)
        {
            var region = AwsStartup.ParseRegion(parameters.AwsRegion);
            var creds = AwsStartup.CredentialsWithFallback(
                parameters.AwsAccessKey, parameters.AwsSecretKey, parameters.AwsProfile);

            For<IAmazonDynamoDB>()
                .Use(ctx => new AmazonDynamoDBClient(creds, new AmazonDynamoDBConfig {RegionEndpoint = region}))
                .Singleton();
            For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds, new AmazonCloudWatchConfig {RegionEndpoint = region}))
                .Singleton();
            For<IAmazonSimpleNotificationService>()
                .Use(ctx => new AmazonSimpleNotificationServiceClient(creds,
                    new AmazonSimpleNotificationServiceConfig {RegionEndpoint = region}))
                .Singleton();
            For<IAmazonRDS>()
                .Use(ctx => new AmazonRDSClient(creds, new AmazonRDSConfig {RegionEndpoint = region}))
                .Singleton();
            For<IAmazonAutoScaling>()
                .Use(ctx => new AmazonAutoScalingClient(creds, region))
                .Singleton();
            For<IAmazonCloudFormation>()
                .Use(ctx => new AmazonCloudFormationClient(creds, region))
                .Singleton();
            For<IAmazonLambda>()
                .Use(ctx => new AmazonLambdaClient(creds, region))
                .Singleton();
            For<IAmazonEC2>()
                .Use(ctx => new AmazonEC2Client(creds, region))
                .Singleton();
            For<IAmazonElasticLoadBalancing>()
                .Use(ctx => new AmazonElasticLoadBalancingClient(creds, region))
                .Singleton();
            For<IAmazonS3>()
                .Use(ctx => new AmazonS3Client(creds, region))
                .Singleton();
            For<IAmazonStepFunctions>()
                .Use(ctx => new AmazonStepFunctionsClient(creds, region))
                .Singleton();
            For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds, region))
                .Singleton();
        }
    }
}
