using System;
using Amazon.Runtime;

namespace TestHelper
{
    public static class CredentialsReader
    {
        private static AWSCredentials _credentials;

        public static AWSCredentials GetCredentials()
        {
            if (_credentials == null)
            {
                _credentials = MakeCredentials();
            }

            return _credentials;
        }

        private static AWSCredentials MakeCredentials()
        {
            var accessKey = Environment.GetEnvironmentVariable("AwsAccessKey");
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new CredentialsException("No AwsAccessKey found");
            }

            var secretKey = Environment.GetEnvironmentVariable("AwsSecretKey");

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new CredentialsException("No AwsSecretKey found");
            }

            return new BasicAWSCredentials(accessKey, secretKey);
        }
    }
}
