using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AutoScaling.Model;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.AutoScaling
{
    public class AutoScalingGroupAlarmDataProvider : IAlarmDimensionProvider<AutoScalingGroup, AutoScalingResourceConfig>,
        IResourceAttributesProvider<AutoScalingGroup, AutoScalingResourceConfig>
    {
        private static string GetAttribute(AutoScalingGroup resource, string property)
        {
            switch (property)
            {
                case "AutoScalingGroupName":
                   return resource.AutoScalingGroupName;

                default:
                    throw new Exception("Unsupported dimension " + property);
            }
        }

        public decimal GetValue(AutoScalingGroup resource, AutoScalingResourceConfig config, string property)
        {
            switch (property)
            {
                case "GroupDesiredCapacity":
                    return resource.DesiredCapacity;
            }

            throw new Exception("Unsupported property name");
        }

        private static Dimension GetDimension(AutoScalingGroup resource, string dimensionName)
        {
            return new Dimension
                {
                    Name = dimensionName,
                    Value = GetAttribute(resource, dimensionName)
                };
        }

        public List<Dimension> GetDimensions(AutoScalingGroup resource, AutoScalingResourceConfig config, IList<string> dimensionNames)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }
    }
}
