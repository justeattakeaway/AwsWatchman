using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
using StructureMap;
using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;

namespace Watchman
{
    public static class AwsBootstrapper
    {
        public static void Configure(IProfileRegistry registry, StartupParameters parameters)
        {
            var region = ReadAwsRegion(parameters.AwsRegion);

            var creds = ReadAwsCredentials(parameters);

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
        }

        private static RegionEndpoint ReadAwsRegion(string regionParam)
        {
            if (string.IsNullOrWhiteSpace(regionParam))
            {
                return RegionEndpoint.EUWest1;
            }

            var region = RegionEndpoint.GetBySystemName(regionParam);

            if (string.Equals(region.DisplayName, "Unknown", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ConfigException($"Unknown AWS Region '{regionParam}'");
            }

            return region;
        }

        private static AWSCredentials ReadAwsCredentials(StartupParameters parameters)
        {
            if (CommandLineParamsHasAwsCreds(parameters))
            {
                return new BasicAWSCredentials(parameters.AwsAccessKey, parameters.AwsSecretKey);
            }

            if (! string.IsNullOrWhiteSpace(parameters.AwsProfile))
            {
                return new StoredProfileAWSCredentials(parameters.AwsProfile);
            }

            // use implicit credentials from config or profile
            FallbackCredentialsFactory.CredentialsGenerators = new List<FallbackCredentialsFactory.CredentialsGenerator>
            {
                () => new AppConfigAWSCredentials(),
                () => new StoredProfileAWSCredentials(),
                () => new StoredProfileFederatedCredentials(),
                () => new EnvironmentVariablesAWSCredentials()
            };

            return FallbackCredentialsFactory.GetCredentials(true);
        }

        private static bool CommandLineParamsHasAwsCreds(StartupParameters parameters)
        {
            return
                !string.IsNullOrWhiteSpace(parameters.AwsAccessKey) ||
                !string.IsNullOrWhiteSpace(parameters.AwsSecretKey);
        }
    }
}
