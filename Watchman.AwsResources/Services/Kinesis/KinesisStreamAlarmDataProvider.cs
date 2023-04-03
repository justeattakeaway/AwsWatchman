using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Kinesis
{
    public class KinesisStreamAlarmDataProvider : IAlarmDimensionProvider<KinesisStreamData>,
        IResourceAttributesProvider<KinesisStreamData, ResourceConfig>
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

        public Task<decimal> GetValue(KinesisStreamData resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
