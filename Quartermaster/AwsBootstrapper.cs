using System;
using System.Collections.Generic;
using Amazon;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using StructureMap;
using Watchman.Configuration;

namespace Quartermaster
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

            if (!string.IsNullOrWhiteSpace(parameters.AwsProfile))
            {
                return GetStoredProfile(parameters.AwsProfile);
            }

            // use implicit credentials from config or profile
            FallbackCredentialsFactory.CredentialsGenerators = new List<FallbackCredentialsFactory.CredentialsGenerator>
            {
                () => new AppConfigAWSCredentials(),
                () => GetStoredProfile("default"),
                () => new EnvironmentVariablesAWSCredentials()
            };

            return FallbackCredentialsFactory.GetCredentials(true);
        }

        private static AWSCredentials GetStoredProfile(string profileName)
        {
            CredentialProfile basicProfile;
            var sharedFile = new SharedCredentialsFile();
            var gotProfileByName = sharedFile.TryGetProfile(profileName, out basicProfile);

            if (gotProfileByName)
            {
                AWSCredentials awsCredentials;
                if (AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
                {
                    return awsCredentials;
                }
            }

            return null;
        }

        private static bool CommandLineParamsHasAwsCreds(StartupParameters parameters)
        {
            return
                !string.IsNullOrWhiteSpace(parameters.AwsAccessKey) ||
                !string.IsNullOrWhiteSpace(parameters.AwsSecretKey);
        }
    }
}
