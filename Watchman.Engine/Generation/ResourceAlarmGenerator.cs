using Watchman.AwsResources;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    public class ResourceAlarmGenerator<T, TAlarmConfig> : IResourceAlarmGenerator<T, TAlarmConfig>
        where T:class
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        private readonly IAlarmDimensionProvider<T> _dimensions;
        private readonly AlarmDefaults<T> _defaultAlarms;
        private readonly IResourceAttributesProvider<T, TAlarmConfig> _attributeProvider;

        public ResourceAlarmGenerator(
            IAlarmDimensionProvider<T> dimensionProvider,
            AlarmDefaults<T> defaultAlarms,
            IResourceAttributesProvider<T, TAlarmConfig> attributeProvider)
        {
            _dimensions = dimensionProvider;
            _defaultAlarms = defaultAlarms;
            _attributeProvider = attributeProvider;
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            PopulatedServiceAlarms<TAlarmConfig, T> service,
            AlertingGroupParameters groupParameters)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }

            List<Alarm> alarms = new List<Alarm>();

            foreach (var resource in service.Resources)
            {
                var alarmsForResource = await CreateAlarmsForResource(resource, service,
                    groupParameters);
                alarms.AddRange(alarmsForResource);
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            ResourceAndThresholdsPair<TAlarmConfig, T> resource,
            PopulatedServiceAlarms<TAlarmConfig, T> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await resource.Resource.GetFullResource();

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Resource.Name} not found");
            }

            var mergedConfig = service.Options.OverrideWith(resource.Definition.Options);
            var mergedValuesByAlarmName = service.Values.OverrideWith(resource.Definition.Values);

            var result = new List<Alarm>();

            foreach (var alarm in _defaultAlarms)
            {
                var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();
                var configuredThreshold = alarm.Threshold.CopyWith(value: values.Threshold);
                var dimensions = _dimensions.GetDimensions(entity, alarm.DimensionNames);
                var threshold = await ThresholdCalculator.ExpandThreshold(_attributeProvider,
                    entity,
                    mergedConfig,
                    configuredThreshold);

                var built = alarm.CopyWith(threshold, values);

                var model = new Alarm
                {
                    AlarmName = $"{resource.Resource.Name}-{built.Name}-{groupParameters.AlarmNameSuffix}",
                    AlarmDescription = groupParameters.DefaultAlarmDescription(resource.Definition),
                    ResourceIdentifier = resource.Resource.Name,
                    Dimensions = dimensions,
                    AlarmDefinition = built
                };

                result.Add(model);
            }

            return result;
        }
    }
}
