using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbSource : ResourceSourceBase<LoadBalancer>
    {
        private readonly IAmazonElasticLoadBalancingV2 _client;

        public AlbSource(IAmazonElasticLoadBalancingV2 client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected override async Task<IEnumerable<LoadBalancer>> FetchResources()
        {
            var results = new List<LoadBalancer>();
            string marker = null;

            do
            {
                var response = await _client.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest
                {
                    Marker = marker
                });

                var applicationLoadBalancers = response.LoadBalancers
                    .Where(x => x.Type == LoadBalancerTypeEnum.Application);

                results.AddRange(applicationLoadBalancers);
                marker = response.NextMarker;
            }
            while (!string.IsNullOrEmpty(marker));

            return results;
        }

        protected override string GetResourceName(LoadBalancer resource) => resource.LoadBalancerName;
    }
}
