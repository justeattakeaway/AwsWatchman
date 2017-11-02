using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources.Services.Kinesis
{
    public class KinesisStreamAlarmDataProvider : IAlarmDimensionProvider<KinesisStreamData>, IResourceAttributesProvider<KinesisStreamData>
    {
        public List<Dimension> GetDimensions(KinesisStreamData resource, IList<string> dimensionNames) =>
            dimensionNames.Select(x => GetDimension(resource, x))
                .ToList();

        private Dimension GetDimension(KinesisStreamData resource, string dimensionName)
        {
            switch (dimensionName)
            {
                case "StreamName":
                    return new Dimension { Name = "StreamName", Value = resource.Name };

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }
        }

        public decimal GetValue(KinesisStreamData resource, string property)
        {
            throw new NotImplementedException();
        }
    }
}
