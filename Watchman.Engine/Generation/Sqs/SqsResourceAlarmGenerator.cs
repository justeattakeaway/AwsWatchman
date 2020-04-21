using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Generation.Sqs
{
    public class SqsResourceAlarmGenerator : IResourceAlarmGenerator<QueueDataV2, SqsResourceConfig>
    {
        private readonly IAlarmDimensionProvider<QueueDataV2> _dimensionProvider;
        private readonly IResourceAttributesProvider<QueueDataV2, SqsResourceConfig> _attributeProvider;
        private readonly IList<AlarmDefinition> _errorQueueDefaults = Defaults.SqsError;
        private readonly AlarmDefaults<QueueDataV2> _defaultAlarms;


        public SqsResourceAlarmGenerator(
            IAlarmDimensionProvider<QueueDataV2> dimensionProvider,
            IResourceAttributesProvider<QueueDataV2, SqsResourceConfig> attributeProvider, AlarmDefaults<QueueDataV2> defaultAlarms)
        {
            _dimensionProvider = dimensionProvider;
            _attributeProvider = attributeProvider;
            _defaultAlarms = defaultAlarms;
        }

        public async Task<IList<Alarm>> GenerateAlarmsFor(
            PopulatedServiceAlarms<SqsResourceConfig, QueueDataV2> service,
            AlertingGroupParameters groupParameters)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            var alarms = new List<Alarm>();

            foreach (var resource in service.Resources)
            {
                var alarmsForResource = await CreateAlarmsForResource(resource, service, groupParameters);
                alarms.AddRange(alarmsForResource);
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            ResourceAndThresholdsPair<SqsResourceConfig, QueueDataV2> resource,
            PopulatedServiceAlarms<SqsResourceConfig, QueueDataV2> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await resource.Resource.GetFullResource();

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Resource.Name} not found");
            }

            var mergedConfig = service.Options.OverrideWith(resource.Definition.Options);
            var includeErrorQueues = mergedConfig.IncludeErrorQueues ?? true;

            var result = new List<Alarm>();

            // get working queue alarms
            if (entity.WorkingQueue != null)
            {
                var alarms = await BuildAlarmsForQueue(_defaultAlarms,
                    mergedConfig,
                    resource.Definition,
                    service,
                    groupParameters,
                    entity.WorkingQueue);

                result.AddRange(alarms);
            }

            // get error queue alarms
            if (includeErrorQueues && entity.ErrorQueue != null)
            {
                var alarms = await BuildAlarmsForQueue(_errorQueueDefaults,
                    mergedConfig,
                    resource.Definition,
                    service,
                    groupParameters,
                    entity.ErrorQueue);

                result.AddRange(alarms);
            }

            return result;
        }

        private async Task<List<Alarm>> BuildAlarmsForQueue(
            IList<AlarmDefinition> defaults,
            SqsResourceConfig sqsConfig,
            ResourceThresholds<SqsResourceConfig> resource,
            PopulatedServiceAlarms<SqsResourceConfig, QueueDataV2> service,
            AlertingGroupParameters groupParameters,
            QueueDataV2 queue)
        {
            var result = new List<Alarm>();

            var mergedValuesByAlarmName = service.Values.OverrideWith(resource.Values);

            foreach (var alarm in defaults)
            {
                var dimensions = _dimensionProvider.GetDimensions(queue, alarm.DimensionNames);
                var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();

                var actualThreshold = alarm.Threshold.CopyWith(value: values.Threshold);

                var threshold = await ThresholdCalculator.ExpandThreshold(_attributeProvider,
                    queue,
                    sqsConfig,
                    actualThreshold);

                var built = alarm.CopyWith(threshold, values);

                var model = new Alarm
                {
                    AlarmName = $"{resource.Name}-{built.Name}-{groupParameters.AlarmNameSuffix}",
                    AlarmDescription = groupParameters.DefaultAlarmDescription(resource),

                    // error queues currently named as per parent queue
                    ResourceIdentifier = resource.Name,
                    Dimensions = dimensions,
                    AlarmDefinition = built
                };

                result.Add(model);
            }

            return result;
        }
    }
}
