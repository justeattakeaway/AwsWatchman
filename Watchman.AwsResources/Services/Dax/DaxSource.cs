using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DAX;
using Amazon.DAX.Model;

namespace Watchman.AwsResources.Services.Dax
{
    public class DaxSource : ResourceSourceBase<Cluster>
    {
        private readonly IAmazonDAX _amazonDax;

        public DaxSource(IAmazonDAX amazonDax)
        {
            _amazonDax = amazonDax;
        }

        protected override async Task<IEnumerable<Cluster>> FetchResources()
        {
            var results = new List<IEnumerable<Cluster>>();
            string nextToken = null;

            do
            {
                var response = await _amazonDax.DescribeClustersAsync(new DescribeClustersRequest
                {
                    NextToken = nextToken
                });

                results.Add(response.Clusters);
                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            return results.SelectMany(x => x).ToList();
        }

        protected override string GetResourceName(Cluster resource) => resource.ClusterName;
    }
}
