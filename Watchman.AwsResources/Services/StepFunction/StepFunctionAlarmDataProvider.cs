using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;
using Amazon.StepFunctions.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.StepFunction
{
    public class StepFunctionAlarmDataProvider : IAlarmDimensionProvider<StateMachineListItem, ResourceConfig>,
        IResourceAttributesProvider<StateMachineListItem>
    {
        public List<Dimension> GetDimensions(StateMachineListItem resource, ResourceConfig config, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        private static Dimension GetDimension(StateMachineListItem resource, string dimensionName)
        {
            return new Dimension
            {
                Name = dimensionName,
                Value = GetAttribute(resource, dimensionName)
            };
        }

        private static string GetAttribute(StateMachineListItem resource, string dimensionName)
        {
            switch (dimensionName)
            {
                case "StateMachineArn":
                    return resource.StateMachineArn;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }
        }

        public decimal GetValue(StateMachineListItem resource, string property)
        {
            throw new NotImplementedException();
        }
    }
}
