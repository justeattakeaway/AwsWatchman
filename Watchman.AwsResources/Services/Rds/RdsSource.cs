using Amazon.RDS;
using Amazon.RDS.Model;

namespace Watchman.AwsResources.Services.Rds
{
    public class RdsSource : ResourceSourceBase<DBInstance>
    {
        private readonly IAmazonRDS _client;

        public RdsSource(IAmazonRDS client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        protected override async Task<IEnumerable<DBInstance>> FetchResources()
        {
            var results = new List<IEnumerable<DBInstance>>();
            string marker = null;

            do
            {
                var response = await _client.DescribeDBInstancesAsync(new DescribeDBInstancesRequest
                {
                    Marker = marker
                });

                results.Add(response.DBInstances);
                marker = response.Marker;
            }
            while (!string.IsNullOrEmpty(marker));

            return results.SelectMany(x => x).ToList();
        }

        protected override string GetResourceName(DBInstance resource) => resource.DBInstanceIdentifier;
    }
}
