using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    public class ResourceAlarmGenerator<T, TAlarmConfig> : IResourceAlarmGenerator<T, TAlarmConfig>
        where T:class
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        private readonly IResourceSource<T> _tableSource;
        private readonly IAlarmDimensionProvider<T> _dimensions;
        private readonly AlarmBuilder<T, TAlarmConfig> _builder; 
        
        public ResourceAlarmGenerator(
            IResourceSource<T> tableSource,
            IAlarmDimensionProvider<T> dimensionProvider,
            IResourceAttributesProvider<T, TAlarmConfig> attributeProvider)
        {
            _tableSource = tableSource;
            _dimensions = dimensionProvider;
            _builder = new AlarmBuilder<T, TAlarmConfig>(attributeProvider);
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            AwsServiceAlarms<TAlarmConfig> service,
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
                var alarmsForResource = await CreateAlarmsForResource(defaults, resource, service, groupParameters);
                alarms.AddRange(alarmsForResource);                    
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<TAlarmConfig> resource,
            AwsServiceAlarms<TAlarmConfig> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await _tableSource.GetResourceAsync(resource.Name);

            // apply thresholds from resource or alerting group
            var expanded = await _builder.CopyAndUpdateDefaultAlarmsForResource(entity, defaults, service, resource);

            var result = new List<Alarm>();
            
            if (entity == null)
            {
                throw new Exception($"Entity {resource.Name} not found");
            }

            foreach (var alarm in expanded)
            {
                var dimensions = _dimensions.GetDimensions(entity.Resource, alarm.DimensionNames);

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
