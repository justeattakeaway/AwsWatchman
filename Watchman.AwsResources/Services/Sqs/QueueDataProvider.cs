using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.Lambda.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueDataProvider : IAlarmDimensionProvider<QueueData>, IResourceAttributesProvider<QueueData, SqsResourceConfig>
    {
        public List<Dimension> GetDimensions(QueueData resource, IList<string> dimensionNames)
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

        public Task<decimal> GetValue(QueueData resource, SqsResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
