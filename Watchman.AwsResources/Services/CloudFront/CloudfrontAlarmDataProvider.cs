using Amazon.CloudFront.Model;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;


namespace Watchman.AwsResources.Services.CloudFront
{
    public class CloudFrontAlarmDataProvider : IAlarmDimensionProvider<DistributionSummary>,
        IResourceAttributesProvider<DistributionSummary, ResourceConfig>
    {
        public List<Dimension> GetDimensions(DistributionSummary resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        public Task<decimal> GetValue(DistributionSummary resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }

        private Dimension GetDimension(DistributionSummary resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "DistributionId":
                    dim.Value = resource.Id;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }
    }
}
