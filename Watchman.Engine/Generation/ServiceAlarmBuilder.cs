using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    public class ServiceAlarmBuilder<T, TAlarmConfig>
        where T:class
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        private readonly IResourceSource<T> _tableSource;
        private readonly IAlarmDimensionProvider<T, TAlarmConfig> _dimensions;
        private readonly IResourceAttributesProvider<T> _attributes;

        public ServiceAlarmBuilder(
            IResourceSource<T> tableSource,
            IAlarmDimensionProvider<T, TAlarmConfig> dimensionProvider,
            IResourceAttributesProvider<T> attributeProvider)
        {
            _tableSource = tableSource;
            _dimensions = dimensionProvider;
            _attributes = attributeProvider;
        }

        private Threshold ExpandThreshold(T resource, Threshold threshold)
        {
            if (threshold.ThresholdType == ThresholdType.PercentageOf)
            {
                var fraction = threshold.Value / 100;
                var property = _attributes.GetValue(resource, threshold.SourceAttribute);

                threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = fraction * (double) property
                };
            }

            return threshold;
        }

        private IList<AlarmDefinition> ExpandDefaultAlarmsForResource(IList<AlarmDefinition> alerts,
            params Dictionary<string, AlarmValues>[] thresholds)
        {
            return alerts
                .Select(x => AlarmWithMergedValues(x, thresholds))
                .ToList();
        }

        private static AlarmDefinition AlarmWithMergedValues(
            AlarmDefinition alarm,
            Dictionary<string, AlarmValues>[] thresholds)
        {
            var mergedThreshold = MergeValueOverrides(alarm.Name, thresholds);

            var copy = alarm.Copy();

            copy.Threshold = new Threshold
            {
                SourceAttribute = alarm.Threshold.SourceAttribute,
                ThresholdType = alarm.Threshold.ThresholdType,
                Value = mergedThreshold.Threshold ?? alarm.Threshold.Value
            };

            copy.EvaluationPeriods = mergedThreshold.EvaluationPeriods ?? alarm.EvaluationPeriods;

            return copy;
        }

        private static AlarmValues MergeValueOverrides(string key, Dictionary<string, AlarmValues>[] thresholds)
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

            return new AlarmValues(matchedThresholdValue, matchedEvalPeriods);
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            AwsServiceAlarms<TAlarmConfig> service,
            IList<AlarmDefinition> defaults,
            string alarmSuffix)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            var allAlarms = await Task.WhenAll(service.Resources
                .Select(r => ExpandAlarmsToResources(
                    defaults,
                    r,
                    service,
                    alarmSuffix)));

            return allAlarms.SelectMany(x => x).ToList();
        }

        private async Task<IList<Alarm>> ExpandAlarmsToResources(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<TAlarmConfig> resource,
            AwsServiceAlarms<TAlarmConfig> service,
            string groupSuffix)
        {
            // apply thresholds from resource or alerting group
            var expanded = ExpandDefaultAlarmsForResource(defaults, resource.Values, service.Values);

            var config = MergeConfiguration(service.Parameters, resource.Parameters);
            
            return await GetAlarms(expanded, resource, config, groupSuffix);
        }

        private string GetAlarmName(AwsResource<T> resource, string alertName, string groupSuffix)
        {
            return $"{resource.Name}-{alertName}-{groupSuffix}";
        }

        private async Task<IList<Alarm>> GetAlarms(IList<AlarmDefinition> alarms,
            ResourceThresholds<TAlarmConfig> awsResource,
            TAlarmConfig configuration,
            string groupSuffix)
        {
            var result = new List<Alarm>();

            var entity = await _tableSource.GetResourceAsync(awsResource.Name);

            if (entity == null)
            {
                throw new Exception($"Entity {awsResource.Name} not found");
            }

            // expand dynamic thresholds
            foreach (var alarm in alarms)
            {
                alarm.Threshold = ExpandThreshold(entity.Resource, alarm.Threshold);
                
                var dimensions = _dimensions.GetDimensions(entity.Resource, configuration, alarm.DimensionNames);

                var model = new Alarm
                {
                    AlarmName = GetAlarmName(entity, alarm.Name, groupSuffix),
                    Resource = entity,
                    Dimensions = dimensions,
                    AlarmDefinition = alarm
                };
                result.Add(model);
            }

            return result;
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
    }
}
