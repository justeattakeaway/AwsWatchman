using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    class AlarmBuilder<T, TAlarmConfig>
        where T : class
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        private readonly IResourceAttributesProvider<T, TAlarmConfig> _attributeProvider;

        public AlarmBuilder(IResourceAttributesProvider<T, TAlarmConfig> attributeProvider)
        {
            _attributeProvider = attributeProvider;
        }

        private TAlarmConfig MergeConfiguration(TAlarmConfig serviceLevel, TAlarmConfig resourceLevel)
        {
            if (resourceLevel == null)
            {
                return serviceLevel ?? new TAlarmConfig();
            }

            if (serviceLevel == null)
            {
                return resourceLevel;
            }

            return resourceLevel.Merge(serviceLevel);
        }

        private AlarmValues MergeValueOverrides(string key, Dictionary<string, AlarmValues>[] thresholds)
        {
            var matchesForKey = thresholds
                .Where(t => t != null && t.ContainsKey(key))
                .Select(t => t[key])
                .ToList();

            var matchedEvalPeriods = matchesForKey
                .Select(t => t.EvaluationPeriods)
                .FirstOrDefault(t => t.HasValue);

            var matchedThresholdValue = matchesForKey
                .Select(t => t.Threshold)
                .FirstOrDefault(t => t.HasValue);

            var matchedExtendedStatistic = matchesForKey
                .Select(t => t.ExtendedStatistic)
                .FirstOrDefault(t => !string.IsNullOrEmpty(t));

            return new AlarmValues(matchedThresholdValue, matchedEvalPeriods, matchedExtendedStatistic);
        }

        private async Task<AlarmDefinition> AlarmWithMergedValues(
            AwsResource<T> entity,
            AlarmDefinition alarm,
            AwsServiceAlarms<TAlarmConfig> serviceConfig,
            ResourceThresholds<TAlarmConfig> resourceConfig)
        {
            var mergedThreshold = MergeValueOverrides(alarm.Name, new [] { resourceConfig.Values, serviceConfig.Values }) ;
            var config = MergeConfiguration(serviceConfig.Options, resourceConfig.Options);

            var copy = alarm.Copy();

            copy.Threshold = await ExpandThreshold(entity.Resource, config, new Threshold
            {
                SourceAttribute = alarm.Threshold.SourceAttribute,
                ThresholdType = alarm.Threshold.ThresholdType,
                Value = mergedThreshold.Threshold ?? alarm.Threshold.Value
            });

            copy.EvaluationPeriods = mergedThreshold.EvaluationPeriods ?? alarm.EvaluationPeriods;

            copy.ExtendedStatistic = !string.IsNullOrEmpty(mergedThreshold.ExtendedStatistic)
                ? mergedThreshold.ExtendedStatistic
                : alarm.ExtendedStatistic;

            return copy;
        }

        public async Task<IList<AlarmDefinition>> CopyAndUpdateDefaultAlarmsForResource(
            AwsResource<T> entity,
            IList<AlarmDefinition> alerts,
            AwsServiceAlarms<TAlarmConfig> serviceConfig,
            ResourceThresholds<TAlarmConfig> resourceConfig)
        {
            var result = new List<AlarmDefinition>();

            foreach (var defaultAlarm in alerts)
            {
                var alarm = await AlarmWithMergedValues(entity, defaultAlarm, serviceConfig, resourceConfig);
                result.Add(alarm);
            }

            return result;
        }
        
        private async Task<Threshold> ExpandThreshold(T resource, TAlarmConfig config, Threshold threshold)
        {
            if (threshold.ThresholdType == ThresholdType.PercentageOf)
            {
                var fraction = threshold.Value / 100;
                var property = await _attributeProvider.GetValue(resource, config, threshold.SourceAttribute);

                threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = fraction * (double)property
                };
            }

            return threshold;
        }

        public string GetAlarmName(AwsResource<T> resource, string alertName, string groupSuffix)
        {
            return $"{resource.Name}-{alertName}-{groupSuffix}";
        }
    }
}
