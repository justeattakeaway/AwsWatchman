using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    public class ServiceAlarmBuilder<T> where T:class
    {
        private readonly IResourceSource<T> _tableSource;
        private readonly IAlarmDimensionProvider<T> _dimensions;
        private readonly IResourceAttributesProvider<T> _attributes;

        public ServiceAlarmBuilder(
            IResourceSource<T> tableSource,
            IAlarmDimensionProvider<T> dimensionProvider,
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
            params Dictionary<string, ThresholdValue>[] thresholds)
        {
            return alerts
                .Select(x => AlarmWithMergedThreshold(x, thresholds))
                .ToList();
        }

        private static AlarmDefinition AlarmWithMergedThreshold(
            AlarmDefinition alarm,
            Dictionary<string, ThresholdValue>[] thresholds)
        {
            var copy = alarm.Copy();
            copy.Threshold = MergeThreshold(alarm, thresholds);
            return copy;
        }

        private static Threshold MergeThreshold(AlarmDefinition x, Dictionary<string, ThresholdValue>[] thresholds)
        {
            var key = x.Name;
            var mergedThreshold = thresholds
                .Where(t => t != null && t.ContainsKey(key))
                .Select(t => t[key].Value)
                .DefaultIfEmpty(x.Threshold.Value)
                .FirstOrDefault();

            return new Threshold
            {
                SourceAttribute = x.Threshold.SourceAttribute,
                ThresholdType = x.Threshold.ThresholdType,
                Value = mergedThreshold
            };
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(ServiceAlertingGroup alertingGroup, string snsTopicArn, IList<AlarmDefinition> defaults)
        {
            var service = alertingGroup.Service;

            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            var allAlarms = await Task.WhenAll(service.Resources
                .Select(r =>
                {
                    // apply thresholds from resource or alerting group
                    var expanded = ExpandDefaultAlarmsForResource(defaults, r.Thresholds,
                        service.Thresholds);
                    return GetAlarms(expanded, r, snsTopicArn, alertingGroup);
                }));

            return allAlarms.SelectMany(x => x).ToList();
        }

        private string GetAlarmName(AwsResource<T> resource, string alertName, string groupSuffix)
        {
            return $"{resource.Name}-{alertName}-{groupSuffix}";
        }

        private async Task<IList<Alarm>> GetAlarms(IList<AlarmDefinition> alarms,
            ResourceThresholds awsResource,
            string snsTopic,
            ServiceAlertingGroup group)
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
                var dimensions = _dimensions.GetDimensions(entity.Resource, alarm.DimensionNames);

                var model = new Alarm
                {
                    AlarmName = GetAlarmName(entity, alarm.Name, group.AlarmNameSuffix),
                    Resource = entity,
                    AlarmDefinition = alarm,
                    AlertingGroup = group,
                    Dimensions = dimensions,
                    SnsTopicArn = snsTopic
                };
                result.Add(model);
            }

            return result;
        }
    }
}
