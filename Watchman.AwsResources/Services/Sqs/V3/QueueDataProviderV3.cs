using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Sqs.V3
{
    public class QueueDataProviderV3 : IAlarmDimensionProvider<QueueDataV3>, IResourceAttributesProvider<QueueDataV3, SqsResourceConfig>
    {
        public List<Dimension> GetDimensions(QueueDataV3 resource, IList<string> dimensionNames)
        {
            var allowed = new List<Dimension>()
                          {
                              new Dimension()
                              {
                                  Name = "QueueName",
                                  Value = resource.Name
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

        public Task<decimal> GetValue(QueueDataV3 resource, SqsResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
