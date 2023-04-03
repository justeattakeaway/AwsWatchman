﻿using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Validation
{
    public static class AwsServiceValidation
    {
        public static void Validate(string alertingGroupName, string serviceName, IAwsServiceAlarms serviceAlarms)
        {
            if (serviceAlarms.Values != null)
            {
                foreach (var threshold in serviceAlarms.Values)
                {
                    ValidServiceThreshold(threshold);
                }
            }

            if (serviceAlarms.Resources == null)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a '{serviceName}' Service with missing Resources section");
            }

            foreach (var resource in serviceAlarms.Resources)
            {
                ValidateResource(alertingGroupName, serviceName, resource);
            }
        }

        private static void ValidateResource(string agName, string serviceName, IResource resource)
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

            if (resource.Values != null)
            {
                foreach (var threshold in resource.Values)
                {
                    ValidServiceThreshold(threshold);
                }
            }
        }

        private static void ValidServiceThreshold(KeyValuePair<string, AlarmValues> namedThreshold)
        {
            var value = namedThreshold.Value;
            if (value.Threshold <= 0)
            {
                throw new ConfigException($"Threshold of '{namedThreshold.Key}' must be greater than zero");
            }
        }
    }
}
