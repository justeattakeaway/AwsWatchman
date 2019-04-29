using System;
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

            var duplicateNames = config.AlertingGroups
                .Select(g => g.Name)
                .GroupBy(_ => _)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Any())
            {
                throw new ConfigException($"The following alerting group names exist in multiple config files: {string.Join(", ", duplicateNames)}");
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

            ValidateTargets(alertingGroup);

            if (HasAny(alertingGroup.DynamoDb?.Tables))
            {
                DynamoDbValidation.Validate(alertingGroup.Name, alertingGroup.DynamoDb);
            }

            if (HasAny(alertingGroup.Sqs?.Queues))
            {
                SqsValidation.Validate(alertingGroup.Name, alertingGroup.Sqs);
            }

            if (alertingGroup.Services != null)
            {
                foreach (var service in alertingGroup.Services.AllServicesByName)
                {
                    if (service.Value != null)
                    {
                        AwsServiceValidation.Validate(alertingGroup.Name, service.Key, service.Value);
                    }
                }
            }
        }

        private static void ValidateTargets(AlertingGroup alertingGroup)
        {
            if (alertingGroup.Targets == null)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroup.Name}' must have targets");
            }

            foreach (var target in alertingGroup.Targets)
            {
                if (target is AlertEmail)
                {
                    var emailTarget = target as AlertEmail;
                    if (string.IsNullOrWhiteSpace(emailTarget.Email))
                    {
                        throw new ConfigException($"Email target for AlertingGroup '{alertingGroup.Name}' must have an email address");
                    }
                }
                else if (target is AlertUrl)
                {
                    var urlTarget = target as AlertUrl;
                    if (string.IsNullOrWhiteSpace(urlTarget.Url))
                    {
                        throw new ConfigException($"Url target for AlertingGroup '{alertingGroup.Name}' must have a url");
                    }

                    try
                    {
                        new Uri(urlTarget.Url);

                    }
                    catch (UriFormatException e)
                    {
                        throw new ConfigException($"Url target '{urlTarget.Url}' for AlertingGroup '{alertingGroup.Name}' is not valid", e);
                    }
                }
                else
                {
                    throw new ConfigException("Unknown target type");
                }
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
