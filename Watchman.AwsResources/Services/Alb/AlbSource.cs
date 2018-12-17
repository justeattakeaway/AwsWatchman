using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbSource : ResourceSourceBase<AlbResource>
    {
        private readonly IAmazonElasticLoadBalancingV2 _client;

        public AlbSource(IAmazonElasticLoadBalancingV2 client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected override string GetResourceName(AlbResource resource) => resource.LoadBalancer.LoadBalancerName;

        protected override async Task<IEnumerable<AlbResource>> FetchResources()
        {
            var loadBalancers = await GetLoadBalancers();

            var results = loadBalancers.Select(loadBalancer => new AlbResource
            {
                LoadBalancer = loadBalancer
            });

            return results;
        }

        private async Task<IEnumerable<LoadBalancer>> GetLoadBalancers()
        {
            var loadBalancers = new List<LoadBalancer>();
            string marker = null;
            do
            {
                var response = await _client.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest
                {
                    Marker = marker
                });

                var applicationLoadBalancers = response.LoadBalancers
                    .Where(x => x.Type == LoadBalancerTypeEnum.Application);

                loadBalancers.AddRange(applicationLoadBalancers);
                marker = response.NextMarker;
            } while (!string.IsNullOrEmpty(marker));

            return loadBalancers;
        }
    }
}
