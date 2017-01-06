using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;
using Amazon.Lambda.Model;

namespace Watchman.AwsResources.Services.Lambda
{
    public class LambdaAlarmDataProvider : IAlarmDimensionProvider<FunctionConfiguration>, IResourceAttributesProvider<FunctionConfiguration>
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

        public decimal GetValue(FunctionConfiguration resource, string property)
        {
            switch (property)
            {
                case "Timeout":
                    // alarm needs timeout in milliseconds
                    return resource.Timeout * 1000;
            }

            throw new Exception("Unsupported Lambda property name");
        }
    }
}