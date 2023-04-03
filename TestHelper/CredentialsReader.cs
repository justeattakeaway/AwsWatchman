using Amazon.Runtime;
using NUnit.Framework;

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
                Assert.Ignore("No AWS access key configured.");
            }

            var secretKey = Environment.GetEnvironmentVariable("AwsSecretKey");

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Assert.Ignore("No AWS secret key configured.");
            }

            return new BasicAWSCredentials(accessKey, secretKey);
        }
    }
}
