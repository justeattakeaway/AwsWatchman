using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.DynamoDb
{
    public class DynamoDbGsiDataProvider : IResourceAttributesProvider<GlobalSecondaryIndexDescription, DynamoResourceConfig>
    {
        public List<Dimension> GetDimensions(GlobalSecondaryIndexDescription resource,
            TableDescription table,
            IList<string> dimensionNames)
        {
            var allowed = new List<Dimension>()
            {
                new Dimension()
                {
                    Name = "GlobalSecondaryIndexName",
                    Value = resource.IndexName
                },
                new Dimension()
                {
                    Name = "TableName",
                    Value = table.TableName
                }
            };

            var requested = dimensionNames
                .Join(allowed, name => name, dim => dim.Name, (_, dim) => dim)
                .ToList();

            if (requested.Count != dimensionNames.Count)
            {
                var missing = dimensionNames
                    .Except(requested.Select(dim => dim.Name))
                    .ToArray();

                throw new Exception($"Requested dimension names are not valid: {string.Join(",", missing)}");
            }

            return requested;
        }

        private const int OneMinuteInSeconds = 60;

        public Task<decimal> GetValue(GlobalSecondaryIndexDescription resource, DynamoResourceConfig config, string property)
        {
            // in future the multiplication by a minute shouldn't be hardcoded
            // it's needed because the read capacity unit is in seconds, but our alarm is currently a sum over 1 minute.

            switch (property)
            {
                case "ProvisionedReadThroughput":
                    return Task.FromResult((decimal) resource.ProvisionedThroughput.ReadCapacityUnits * OneMinuteInSeconds);
                case "ProvisionedWriteThroughput":
                    return Task.FromResult((decimal) resource.ProvisionedThroughput.WriteCapacityUnits * OneMinuteInSeconds);
            }

            throw new ArgumentOutOfRangeException(nameof(property));
        }
    }
}
