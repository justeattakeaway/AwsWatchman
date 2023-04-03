﻿using Amazon.AutoScaling.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.AutoScaling
{
    public class AutoScalingGroupAlarmDataProvider : IAlarmDimensionProvider<AutoScalingGroup>,
        IResourceAttributesProvider<AutoScalingGroup, AutoScalingResourceConfig>
    {
        private readonly IAmazonCloudWatch _cloudWatch;
        private readonly ICurrentTimeProvider _timeProvider;

        public AutoScalingGroupAlarmDataProvider(IAmazonCloudWatch cloudWatch, ICurrentTimeProvider timeProvider = null)
        {
            _cloudWatch = cloudWatch;
            _timeProvider = timeProvider ?? new CurrentTimeProvider();
        }

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

        public async Task<decimal> GetValue(AutoScalingGroup resource, AutoScalingResourceConfig config, string property)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            switch (property)
            {
                case "GroupDesiredCapacity":
                    return await GetGroupDesiredValue(resource, config);
            }

            throw new Exception("Unsupported property name");
        }

        private async Task<decimal> GetGroupDesiredValue(AutoScalingGroup resource, AutoScalingResourceConfig config)
        {
            decimal? result = null;

            if (config.InstanceCountIncreaseDelayMinutes != null)
            {
                result = await FetchLaggedGroupDesiredValueFromCloudWatch(resource, config.InstanceCountIncreaseDelayMinutes.Value);
            }

            return result ?? resource.DesiredCapacity;
        }

        private async Task<decimal?> FetchLaggedGroupDesiredValueFromCloudWatch(AutoScalingGroup resource, int delayMinutes)
        {
            var delaySeconds = delayMinutes * 60;
            var now = _timeProvider.UtcNow;

            var metric = await _cloudWatch.GetMetricStatisticsAsync(
                new GetMetricStatisticsRequest
                {
                    Dimensions = new List<Dimension>
                    {
                        new Dimension
                        {
                            Name = "AutoScalingGroupName",
                            Value = resource.AutoScalingGroupName
                        }
                    },
                    Statistics = new List<string> {"Minimum"},
                    Namespace = "AWS/AutoScaling",
                    Period = delaySeconds,
                    MetricName = "GroupDesiredCapacity",
                    StartTimeUtc = now.AddSeconds(-1 * delaySeconds),
                    EndTimeUtc = now
                }
            );

            var threshold = metric.Datapoints.FirstOrDefault();

            if (threshold == null)
            {
                // ILogger isn't available in this project yet
                Console.WriteLine(
                    $"No datapoints returned when requesting desired capacity from CloudWatch for {resource.AutoScalingGroupName}");

                return null;
            }

            return (decimal) threshold.Minimum;
        }

        private static Dimension GetDimension(AutoScalingGroup resource, string dimensionName)
        {
            return new Dimension
                {
                    Name = dimensionName,
                    Value = GetAttribute(resource, dimensionName)
                };
        }

        public List<Dimension> GetDimensions(AutoScalingGroup resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }
    }
}
