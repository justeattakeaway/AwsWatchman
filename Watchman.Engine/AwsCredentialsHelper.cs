using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Watchman.Configuration;

namespace Watchman.Engine
{
    public static class AwsCredentialsHelper
    {
        public static RegionEndpoint ReadAwsRegion(string regionParam)
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

        public static AWSCredentials ReadAwsCredentials(string awsAccessKey, string awsSecretKey, string awsProfile)
        {
            if (ParamsHasAwsCreds(awsAccessKey, awsSecretKey))
            {
                return new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            }

            if (!string.IsNullOrWhiteSpace(awsProfile))
            {
                return GetStoredProfile(awsProfile);
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

        private static bool ParamsHasAwsCreds(string awsAccessKey, string awsSecretKey)
        {
            return
                !string.IsNullOrWhiteSpace(awsAccessKey) ||
                !string.IsNullOrWhiteSpace(awsSecretKey);
        }
    }
}
