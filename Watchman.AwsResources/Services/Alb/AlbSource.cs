using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbSource : ResourceSourceBase<LoadBalancerDescription>
    {
        private readonly IAmazonElasticLoadBalancing _client;

        public AlbSource(IAmazonElasticLoadBalancing client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        protected override async Task<IEnumerable<LoadBalancerDescription>> FetchResources()
        {
            var results = new List<LoadBalancerDescription>();
            string marker = null;

            do
            {
                var response = await _client.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest
                {
                    Marker = marker
                });

                results.AddRange(response.LoadBalancerDescriptions);
                marker = response.NextMarker;
            }
            while (!string.IsNullOrEmpty(marker));

            return results;
        }

        protected override string GetResourceName(LoadBalancerDescription resource) => resource.LoadBalancerName;
    }
}
