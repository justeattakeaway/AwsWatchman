using System;
using Amazon;
using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
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
            SetupLocalDependencies(parameters);
            SetupAwsDependencies(parameters);
        }

        private TClientConfig CreateClientConfig<TClientConfig>(RegionEndpoint region)
            where TClientConfig : ClientConfig, new()
        {
            var result = new TClientConfig()
            {
                RegionEndpoint = region,
                MaxErrorRetry = 5,
                Timeout = TimeSpan.FromSeconds(30),
                ReadWriteTimeout = TimeSpan.FromSeconds(90)
            };

            return result;
        }

        private void SetupAwsDependencies(StartupParameters parameters)
        {
            var region = AwsStartup.ParseRegion(parameters.AwsRegion);
            var creds = AwsStartup.CredentialsWithFallback(
                parameters.AwsAccessKey, parameters.AwsSecretKey, parameters.AwsProfile);

            For<IAmazonDynamoDB>()
                .Use(ctx => new AmazonDynamoDBClient(creds,
                    CreateClientConfig<AmazonDynamoDBConfig>(region)))
                .Singleton();
            For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds,
                    CreateClientConfig <AmazonCloudWatchConfig>(region)))
                .Singleton();
            For<IAmazonSimpleNotificationService>()
                .Use(ctx => new AmazonSimpleNotificationServiceClient(creds,
                    CreateClientConfig<AmazonSimpleNotificationServiceConfig>(region)))
                .Singleton();
            For<IAmazonRDS>()
                .Use(ctx => new AmazonRDSClient(creds,
                    CreateClientConfig<AmazonRDSConfig>(region)))
                .Singleton();
            For<IAmazonAutoScaling>()
                .Use(ctx => new AmazonAutoScalingClient(creds,
                    CreateClientConfig<AmazonAutoScalingConfig>(region)))
                .Singleton();
            For<IAmazonCloudFormation>()
                .Use(ctx => new AmazonCloudFormationClient(creds,
                    CreateClientConfig<AmazonCloudFormationConfig>(region)))
                .Singleton();
            For<IAmazonLambda>()
                .Use(ctx => new AmazonLambdaClient(creds,
                    CreateClientConfig<AmazonLambdaConfig>(region)))
                .Singleton();
            For<IAmazonEC2>()
                .Use(ctx => new AmazonEC2Client(creds,
                    CreateClientConfig<AmazonEC2Config>(region)))
                .Singleton();
            For<IAmazonElasticLoadBalancing>()
                .Use(ctx => new AmazonElasticLoadBalancingClient(creds,
                    CreateClientConfig<AmazonElasticLoadBalancingConfig>(region)))
                .Singleton();
            For<IAmazonElasticLoadBalancingV2>()
                .Use(ctx => new AmazonElasticLoadBalancingV2Client(creds,
                    CreateClientConfig<AmazonElasticLoadBalancingV2Config>(region)))
                .Singleton();
            For<IAmazonS3>()
                .Use(ctx => new AmazonS3Client(creds,
                    CreateClientConfig<AmazonS3Config>(region)))
                .Singleton();
            For<IAmazonStepFunctions>()
                .Use(ctx => new AmazonStepFunctionsClient(creds,
                    CreateClientConfig<AmazonStepFunctionsConfig>(region)))
                .Singleton();
            For<IAmazonCloudWatch>()
                .Use(ctx => new AmazonCloudWatchClient(creds,
                    CreateClientConfig<AmazonCloudWatchConfig>(region)))
                .Singleton();
        }

        private void SetupLocalDependencies(StartupParameters parameters)
        {
            var alarmLogger = new ConsoleAlarmLogger(parameters.Verbose);
            var loadLogger = new ConsoleConfigLoadLogger(parameters.Verbose);

            For<IAlarmLogger>().Use(alarmLogger);
            For<IConfigLoadLogger>().Use(loadLogger);
            For<IConfigLoader>().Use<ConfigLoader>();
            For<ICurrentTimeProvider>().Use<CurrentTimeProvider>();
        }
    }
}
