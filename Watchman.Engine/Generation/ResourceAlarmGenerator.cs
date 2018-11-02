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
        private readonly AlarmDefaults<T> _defaultAlarms;
        private readonly IResourceAttributesProvider<T, TAlarmConfig> _attributeProvider;

        public ResourceAlarmGenerator(
            IResourceSource<T> tableSource,
            IAlarmDimensionProvider<T> dimensionProvider,
            AlarmDefaults<T> defaultAlarms,
            IResourceAttributesProvider<T, TAlarmConfig> attributeProvider)
        {
            _tableSource = tableSource;
            _dimensions = dimensionProvider;
            _defaultAlarms = defaultAlarms;
            _attributeProvider = attributeProvider;
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            AwsServiceAlarms<TAlarmConfig> service,
            AlertingGroupParameters groupParameters)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            List<Alarm> alarms = new List<Alarm>();

            foreach (var resource in service.Resources)
            {
                var alarmsForResource = await CreateAlarmsForResource(resource, service, groupParameters);
                alarms.AddRange(alarmsForResource);
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            ResourceThresholds<TAlarmConfig> resource,
            AwsServiceAlarms<TAlarmConfig> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await _tableSource.GetResourceAsync(resource.Name);

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Name} not found");
            }

            var mergedConfig = AlarmHelpers.MergeServiceAndResourceConfiguration(service.Options, resource.Options);
            var mergedValuesByAlarmName = service.Values.OverrideWith(resource.Values);

            var result = new List<Alarm>();

            foreach (var alarm in _defaultAlarms)
            {
                var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();

                var dimensions = _dimensions.GetDimensions(entity.Resource, alarm.DimensionNames);
                var built = await AlarmHelpers.AlarmWithMergedValues(_attributeProvider, entity, alarm, mergedConfig,
                    values);

                var model = new Alarm
                {
                    AlarmName = $"{resource.Name}-{built.Name}-{groupParameters.AlarmNameSuffix}",
                    AlarmDescription = AlarmHelpers.GetAlarmDescription(groupParameters),
                    Resource = entity,
                    Dimensions = dimensions,
                    AlarmDefinition = built
                };

                result.Add(model);
            }

            return result;
        }
    }
}
