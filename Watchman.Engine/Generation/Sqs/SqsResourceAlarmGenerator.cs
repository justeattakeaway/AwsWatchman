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
        private readonly AlarmBuilder<QueueDataV2, SqsResourceConfig> _builder;
        private readonly IResourceSource<QueueDataV2> _queueSource;
        private readonly IAlarmDimensionProvider<QueueDataV2> _dimensionProvider;
        private readonly IList<AlarmDefinition> _errorQueueDefaults = Defaults.SqsError;
        private readonly AlarmDefaults<QueueDataV2> _defaultAlarms;


        public SqsResourceAlarmGenerator(
            IResourceSource<QueueDataV2> queueSource,
            IAlarmDimensionProvider<QueueDataV2> dimensionProvider,
            IResourceAttributesProvider<QueueDataV2, SqsResourceConfig> attributeProvider, AlarmDefaults<QueueDataV2> defaultAlarms)
        {
            _builder = new AlarmBuilder<QueueDataV2, SqsResourceConfig>(attributeProvider);
            _queueSource = queueSource;
            _dimensionProvider = dimensionProvider;
            _defaultAlarms = defaultAlarms;
        }

        public async Task<IList<Alarm>> GenerateAlarmsFor(
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            List<Alarm> alarms = new List<Alarm>();

            foreach (var resource in service.Resources)
            {
               var alarmsForResource = await CreateAlarmsForResource(
                    resource?.Options?.IncludeErrorQueues ?? true,
                    resource,
                    service,
                    groupParameters);
                alarms.AddRange(alarmsForResource);
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            bool includeErrorQueues,
            ResourceThresholds<SqsResourceConfig> resource,
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await _queueSource.GetResourceAsync(resource.Name);

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Name} not found");
            }

            var queueResource = new AwsResource<QueueDataV2>(entity.Name, entity.Resource);
            var alarms = await BuildAlarmsForQueue(_defaultAlarms, resource, service, groupParameters, queueResource);

            if (includeErrorQueues && entity.Resource.ErrorQueue != null)
            {
                var errorQueueResource = new AwsResource<QueueDataV2>(entity.Name, entity.Resource.ErrorQueue);
                alarms.AddRange(await BuildAlarmsForQueue(_errorQueueDefaults, resource, service, groupParameters, errorQueueResource));
            }

            return alarms;
        }

        private async Task<List<Alarm>> BuildAlarmsForQueue(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<SqsResourceConfig> resource,
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters,
            AwsResource<QueueDataV2> entity)
        {
            var expanded = await _builder.CopyAndUpdateDefaultAlarmsForResource(entity, defaults, service, resource);

            var result = new List<Alarm>();

            foreach (var alarm in expanded)
            {
                var dimensions = _dimensionProvider.GetDimensions(entity.Resource, alarm.DimensionNames);

                var model = new Alarm
                            {
                                AlarmName = $"{resource.Name}-{alarm.Name}-{groupParameters.AlarmNameSuffix}",
                                AlarmDescription = _builder.GetAlarmDescription(groupParameters),
                                Resource = entity,
                                Dimensions = dimensions,
                                AlarmDefinition = alarm
                            };
                result.Add(model);
            }
            return result;
        }
    }
}
