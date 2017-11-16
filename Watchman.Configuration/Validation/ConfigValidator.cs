using System.Collections.Generic;
using System.Linq;

namespace Watchman.Configuration.Validation
{
    public static class ConfigValidator
    {
        public static void Validate(WatchmanConfiguration config)
        {
            if (config == null)
            {
                throw new ConfigException("Config cannot be null");
            }

            if (!HasAny(config.AlertingGroups))
            {
                throw new ConfigException("Config must have alerting groups");
            }

            foreach (var alertingGroup in config.AlertingGroups)
            {
                Validate(alertingGroup);
            }
        }

        private static void Validate(AlertingGroup alertingGroup)
        {
            if (string.IsNullOrWhiteSpace(alertingGroup.Name))
            {
                throw new ConfigException("AlertingGroup must have a name");
            }

            if (!TextIsValidInSnsTopic(alertingGroup.Name))
            {
                throw new ConfigException($"AlertingGroup name '{alertingGroup.Name}' must be valid in SNS topics");
            }

            if (string.IsNullOrWhiteSpace(alertingGroup.AlarmNameSuffix))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroup.Name}' must have an alarm suffix");
            }

            if (!TextIsValidInSnsTopic(alertingGroup.AlarmNameSuffix))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroup.Name}' must have a suffix valid in SNS topics. '{alertingGroup.AlarmNameSuffix}' is not.");
            }

            if (alertingGroup.Targets == null)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroup.Name}' must have targets");
            }

            var hasAtLeastOneResource = false;

            if (HasAny(alertingGroup.DynamoDb?.Tables))
            {
                hasAtLeastOneResource = true;
                DynamoDbValidation.Validate(alertingGroup.Name, alertingGroup.DynamoDb);
            }

            if (HasAny(alertingGroup.Sqs?.Queues))
            {
                hasAtLeastOneResource = true;
                SqsValidation.Validate(alertingGroup.Name, alertingGroup.Sqs);
            }

            if (HasAny(alertingGroup.Services))
            {
                foreach (var service in alertingGroup.Services)
                {
                    if (HasAny(service.Value?.Resources))
                    {
                        hasAtLeastOneResource = true;
                        AwsServiceValidation.Validate(alertingGroup.Name, service.Key, service.Value);
                    }
                }
            }

            if (!hasAtLeastOneResource)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroup.Name}' must contain resources to monitor. " +
                    "Specify one or more of DynamoDb, Sqs or other resources");
            }
        }

        private static bool HasAny<T>(IEnumerable<T> values)
        {
            return (values != null) && values.Any();
        }

        /// <summary>
        /// "Topic names are limited to 256 characters.
        /// Alphanumeric characters plus hyphens (-) and underscores (_) are allowed."
        /// https://aws.amazon.com/sns/faqs/
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TextIsValidInSnsTopic(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length > 100)
            {
                return false;
            }

            return value.All(c => IsAllowedChar(c));
        }

        private static bool IsAllowedChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '_';
        }
    }
}
