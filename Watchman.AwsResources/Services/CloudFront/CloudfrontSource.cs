using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace Watchman.AwsResources.Services.CloudFront
{
    public class CloudFrontSource : ResourceSourceBase<DistributionSummary>
    {
        private readonly IAmazonCloudFront _amazonCloudFront;

        public CloudFrontSource(IAmazonCloudFront amazonCloudFront)
        {
            _amazonCloudFront = amazonCloudFront;
        }

        protected override string GetResourceName(DistributionSummary resource)
        {
            return resource.Id;
        }

        protected override async Task<IEnumerable<DistributionSummary>> FetchResources()
        {
            var results = new List<IEnumerable<DistributionSummary>>();
            string nextMarker = null;

            do
            {
                var response = await _amazonCloudFront.ListDistributionsAsync(new ListDistributionsRequest()
                {
                    Marker = nextMarker
                });

                results.Add(response.DistributionList.Items.ToList());
                nextMarker = response.DistributionList.NextMarker;
            } while (!string.IsNullOrEmpty(nextMarker));

            return results.SelectMany(x => x).ToList();
        }
    }
}
