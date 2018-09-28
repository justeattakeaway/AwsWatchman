using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Generation.Sqs
{
    public class SqsResourceAlarmGenerator : IResourceAlarmGenerator<QueueData, SqsResourceConfig>
    {
        private readonly AlarmBuilder<QueueData, SqsResourceConfig> _builder;
        private readonly AlarmBuilder<ErrorQueueData, SqsResourceConfig> _errorQueueBuilder;
        private readonly IResourceSource<QueueData> _queueSource;
        private readonly IAlarmDimensionProvider<QueueData> _dimensionProvider;
        private readonly IAlarmDimensionProvider<ErrorQueueData> _errorDimensionProvider;
        private readonly IList<AlarmDefinition> _errorQueueDefaults = Defaults.SqsError;

        public SqsResourceAlarmGenerator(
            IResourceSource<QueueData> queueSource,
            IAlarmDimensionProvider<QueueData> dimensionProvider,
            IResourceAttributesProvider<QueueData, SqsResourceConfig> attributeProvider,
            IAlarmDimensionProvider<ErrorQueueData> errorDimensionProvider,
            IResourceAttributesProvider<ErrorQueueData, SqsResourceConfig> errorAttributesProvider)
        {
            _builder = new AlarmBuilder<QueueData, SqsResourceConfig>(attributeProvider);
            _errorQueueBuilder = new AlarmBuilder<ErrorQueueData, SqsResourceConfig>(errorAttributesProvider);
            _queueSource = queueSource;
            _dimensionProvider = dimensionProvider;
            _errorDimensionProvider = errorDimensionProvider;
        }

        public async Task<IList<Alarm>> GenerateAlarmsFor(
            AwsServiceAlarms<SqsResourceConfig> service,
            IList<AlarmDefinition> defaults,
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
                    defaults,
                    resource,
                    service,
                    groupParameters);
                alarms.AddRange(alarmsForResource);
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            bool includeErrorQueues,
            IList<AlarmDefinition> defaults,
            ResourceThresholds<SqsResourceConfig> resource,
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters)
        { 
            var entity = await _queueSource.GetResourceAsync(resource.Name);

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Name} not found");
            }

            var alarms = await BuildAlarmsForQueue(defaults, resource, service, groupParameters, entity);

            if (includeErrorQueues && entity.Resource.ErrorQueue != null)
            {
                var errorQueueResource = new AwsResource<ErrorQueueData>(entity.Name, entity.Resource.ErrorQueue);
                alarms.AddRange(await BuildAlarmsForErrorQueue(_errorQueueDefaults, resource, service, groupParameters, errorQueueResource));
            }
            
            return alarms;
        }

        private async Task<List<Alarm>> BuildAlarmsForQueue(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<SqsResourceConfig> resource,
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters,
            AwsResource<QueueData> entity)
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

        private async Task<List<Alarm>> BuildAlarmsForErrorQueue(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<SqsResourceConfig> resource,
            AwsServiceAlarms<SqsResourceConfig> service,
            AlertingGroupParameters groupParameters,
            AwsResource<ErrorQueueData> entity)
        {
            var expanded = await _errorQueueBuilder.CopyAndUpdateDefaultAlarmsForResource(entity, defaults, service, resource);

            var result = new List<Alarm>();

            foreach (var alarm in expanded)
            {
                var dimensions = _errorDimensionProvider.GetDimensions(entity.Resource, alarm.DimensionNames);

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
