using Amazon.CloudWatch.Model;
using Amazon.Lambda.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Lambda
{
    public class LambdaAlarmDataProvider : IAlarmDimensionProvider<FunctionConfiguration>,
        IResourceAttributesProvider<FunctionConfiguration, ResourceConfig>
    {
        public List<Dimension> GetDimensions(FunctionConfiguration resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        private Dimension GetDimension(FunctionConfiguration resource, string dimensionName)
        {
            var dim = new Dimension
                {
                    Name = dimensionName
                };

            switch (dimensionName)
            {
                case "FunctionName":
                    dim.Value = resource.FunctionName;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }

        public Task<decimal> GetValue(FunctionConfiguration resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "Timeout":
                    // alarm needs timeout in milliseconds
                    decimal result = resource.Timeout * 1000;
                    return Task.FromResult(result);
            }

            throw new Exception("Unsupported Lambda property name");
        }
    }
}
