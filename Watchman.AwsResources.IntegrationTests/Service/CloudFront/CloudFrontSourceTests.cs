using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFront;
using NUnit.Framework;
using TestHelper;
using Watchman.AwsResources.Services.CloudFront;

namespace Watchman.AwsResources.IntegrationTests.Service.CloudFront
{
    public class CloudFrontSourceTests
    {
        [Test]
        public async Task GetResourceNamesAsyncShouldReturnResults()
        {
            var source = InitializeSource();

            var resources = await source.GetResourceNamesAsync();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Is.Not.Empty);
        }

        private static CloudFrontSource InitializeSource()
        {
            var amazonCloudFrontClient = new AmazonCloudFrontClient(RegionEndpoint.EUWest1);
            return new CloudFrontSource(amazonCloudFrontClient);
        }
    }
}
