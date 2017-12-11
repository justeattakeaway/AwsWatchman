using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Watchman.Configuration;

namespace Watchman.Engine
{
    public static class AwsStartup
    {
        public static RegionEndpoint ParseRegion(string regionParam)
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

        /// <summary>
        /// The commandline is expected to contain aws credential info, either
        /// - both of the AWS access key and secret key
        /// - the stored profile name
        /// - none of that, in which case we fall back through sources of implicit creds
        ///   i.e. app config, default profile, environment vars
        /// </summary>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public static AWSCredentials CredentialsWithFallback(
            string accessKey, string secretKey, string profileName)
        {
            if (AwsCredsAreNotEmpty(accessKey, secretKey))
            {
                return new BasicAWSCredentials(accessKey, secretKey);
            }

            if (!string.IsNullOrWhiteSpace(profileName))
            {
                return GetNamedStoredProfile(profileName);
            }

            // use implicit credentials from config or profile
            FallbackCredentialsFactory.CredentialsGenerators = new List<FallbackCredentialsFactory.CredentialsGenerator>
            {
                GetDefaultStoredProfile,
                () => new EnvironmentVariablesAWSCredentials()
            };

            return FallbackCredentialsFactory.GetCredentials(true);
        }

        private static AWSCredentials GetNamedStoredProfile(string profileName)
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            return credentialProfileStoreChain.TryGetAWSCredentials(profileName, out var credentials)
                ? credentials
                : null;
        }

        private static AWSCredentials GetDefaultStoredProfile()
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            return credentialProfileStoreChain.TryGetAWSCredentials("default", out var credentials)
                ? credentials
                : throw new AmazonClientException("Unable to find a default profile in CredentialProfileStoreChain.");
        }

        private static bool AwsCredsAreNotEmpty(string accessKey, string secretKey)
        {
            return
                !string.IsNullOrWhiteSpace(accessKey) ||
                !string.IsNullOrWhiteSpace(secretKey);
        }
    }
}
