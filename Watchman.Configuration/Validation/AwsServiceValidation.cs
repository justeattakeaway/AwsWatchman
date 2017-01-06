using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Validation
{
    public static class AwsServiceValidation
    {
        public static void Validate(string alertingGroupName, string serviceName, AwsServiceAlarms serviceAlarms)
        {
            if (serviceAlarms.Thresholds != null && serviceAlarms.Thresholds.Any())
            {
                foreach (var threshold in serviceAlarms.Thresholds)
                {
                    ValidServiceThreshold(threshold);
                }
            }

            foreach (var resource in serviceAlarms.Resources)
            {
                ValidateResource(alertingGroupName, serviceName, resource);
            }
        }

        private static void ValidateResource(string agName, string serviceName, ResourceThresholds resource)
        {
            if (resource == null)
            {
                throw new ConfigException($"AlertingGroup '{agName}' has a '{serviceName}' Service with null resource");
            }

            if (string.IsNullOrWhiteSpace(resource.Name) && string.IsNullOrWhiteSpace(resource.Pattern))
            {
                throw new ConfigException(
                    $"AlertingGroup '{agName}' has a '{serviceName}' Service with no name or pattern");
            }

            if (resource.Thresholds != null && resource.Thresholds.Any())
            {
                foreach (var threshold in resource.Thresholds)
                {
                    ValidServiceThreshold(threshold);
                }
            }
        }

        private static void ValidServiceThreshold(KeyValuePair<string, double> threshold)
        {
            if (threshold.Value <= 0)
            {
                throw new ConfigException($"Threshold of '{threshold.Key}' must be greater than zero");
            }

            if (threshold.Value > 100000)
            {
                throw new ConfigException($"Threshold of '{threshold.Key}' is ridiculously high");
            }
        }
    }
}
