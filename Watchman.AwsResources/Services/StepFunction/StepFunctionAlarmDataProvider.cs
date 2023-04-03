using Amazon.CloudWatch.Model;
using Amazon.StepFunctions.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.StepFunction
{
    public class StepFunctionAlarmDataProvider : IAlarmDimensionProvider<StateMachineListItem>,
        IResourceAttributesProvider<StateMachineListItem, ResourceConfig>
    {
        public List<Dimension> GetDimensions(StateMachineListItem resource, IList<string> dimensionNames)
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

        public Task<decimal> GetValue(StateMachineListItem resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
