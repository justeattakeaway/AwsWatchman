using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Sqs
{
    public class ErrorQueueDataProvider : IAlarmDimensionProvider<ErrorQueueData>, IResourceAttributesProvider<ErrorQueueData, SqsResourceConfig>
    {
        public List<Dimension> GetDimensions(ErrorQueueData resource, IList<string> dimensionNames)
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


        public Task<decimal> GetValue(ErrorQueueData resource, SqsResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
