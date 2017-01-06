using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;

namespace Watchman.AwsResources.Services.AutoScaling
{
    public class AutoScalingGroupSource : ResourceSourceBase<AutoScalingGroup>
    {
        private readonly IAmazonAutoScaling _client;

        public AutoScalingGroupSource(IAmazonAutoScaling client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        protected override string GetResourceName(AutoScalingGroup group) => group.AutoScalingGroupName;

        protected override async Task<IEnumerable<AutoScalingGroup>> FetchResources()
        {
            var results = new List<IEnumerable<AutoScalingGroup>>();
            string marker = null;

            do
            {
                var response = await _client.DescribeAutoScalingGroupsAsync(
                    new DescribeAutoScalingGroupsRequest
                    {
                        NextToken = marker
                    });

                results.Add(response.AutoScalingGroups);
                marker = response.NextToken;
            }
            while (!string.IsNullOrEmpty(marker));

            return results.SelectMany(x => x).ToList();
        }
    }
}
