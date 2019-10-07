using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.RDS;
using Amazon.RDS.Model;

namespace Watchman.AwsResources.Services.RdsCluster
{
    public class RdsClusterSource : ResourceSourceBase<DBCluster>
    {
        private readonly IAmazonRDS _client;

        public RdsClusterSource(IAmazonRDS client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected override async Task<IEnumerable<DBCluster>> FetchResources()
        {
            var results = new List<IEnumerable<DBCluster>>();
            string marker = null;

            do
            {
                var response = await _client.DescribeDBClustersAsync(new DescribeDBClustersRequest
                {
                    Marker = marker
                });

                results.Add(response.DBClusters);
                marker = response.Marker;
            }
            while (!string.IsNullOrEmpty(marker));

            return results.SelectMany(x => x).ToList();
        }

        protected override string GetResourceName(DBCluster resource) => resource.DBClusterIdentifier;
    }
}
