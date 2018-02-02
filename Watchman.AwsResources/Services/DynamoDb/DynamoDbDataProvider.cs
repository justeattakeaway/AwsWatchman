using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.DynamoDb
{
    public class DynamoDbDataProvider : IAlarmDimensionProvider<TableDescription>,
        IResourceAttributesProvider<TableDescription, ResourceConfig>
    {
        public List<Dimension> GetDimensions(TableDescription resource, IList<string> dimensionNames)
        {
            var allowed = new List<Dimension>()
            {
                new Dimension()
                {
                    Name = "TableName",
                    Value = resource.TableName
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

        public Task<decimal> GetValue(TableDescription resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "ProvisionedReadThroughput":
                    return Task.FromResult((decimal) resource.ProvisionedThroughput.ReadCapacityUnits);
                case "ProvisionedWriteThroughput":
                    return Task.FromResult((decimal) resource.ProvisionedThroughput.WriteCapacityUnits);
            }

            throw new ArgumentOutOfRangeException(nameof(property));
        }
    }
}
