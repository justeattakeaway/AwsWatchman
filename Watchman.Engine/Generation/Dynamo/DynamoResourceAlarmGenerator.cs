using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Dynamo
{
    public class DynamoResourceAlarmGenerator : IResourceAlarmGenerator<TableDescription, DynamoResourceConfig>
    {
        private readonly IAlarmDimensionProvider<TableDescription> _dimensions;
        private readonly IResourceAttributesProvider<TableDescription, DynamoResourceConfig> _attributeProvider;
        private readonly DynamoDbGsiDataProvider _gsiProvider = new DynamoDbGsiDataProvider();
        private readonly DynamoDbDefaults _defaultAlarms;
        private readonly IAlarmLogger _logger;

        public DynamoResourceAlarmGenerator(
            IResourceSource<TableDescription> tableSource,
            IAlarmDimensionProvider<TableDescription> dimensionProvider,
            IResourceAttributesProvider<TableDescription, DynamoResourceConfig> attributeProvider,
            DynamoDbDefaults defaultAlarms,
            IAlarmLogger logger)
        {
            _dimensions = dimensionProvider;
            _attributeProvider = attributeProvider;
            _defaultAlarms = defaultAlarms;
            _logger = logger;
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            PopulatedServiceAlarms<DynamoResourceConfig, TableDescription> service,
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
            ResourceAndThresholdsPair<DynamoResourceConfig, TableDescription> resource,
            PopulatedServiceAlarms<DynamoResourceConfig, TableDescription> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await resource.Resource.GetFullResource();

            if (entity == null)
            {
                _logger.Error($"Skipping table {resource.Resource.Name} as it does not exist");
                return Array.Empty<Alarm>();
            }

            var result = await BuildTableAlarms(resource, service, groupParameters, entity);

            result.AddRange(await BuildIndexAlarms(resource, service, groupParameters, entity));

            return result;
        }

        private async Task<List<Alarm>> BuildTableAlarms(ResourceAndThresholdsPair<DynamoResourceConfig, TableDescription> resourceConfig,
            PopulatedServiceAlarms<DynamoResourceConfig, TableDescription> service,
            AlertingGroupParameters groupParameters,
            TableDescription entity)
        {
            var mergedConfig = service.Options.OverrideWith(resourceConfig.Definition.Options);

            var result = new List<Alarm>();

            var mergedValuesByAlarmName = service.Values.OverrideWith(resourceConfig.Definition.Values);

            var defaults = _defaultAlarms.DynamoDbRead;
            if (mergedConfig.MonitorWrites ?? DynamoResourceConfig.MonitorWritesDefault)
            {
                defaults = defaults.Concat(_defaultAlarms.DynamoDbWrite).ToArray();
            }

            foreach (var alarm in defaults)
            {
                var dimensions = _dimensions.GetDimensions(entity, alarm.DimensionNames);
                var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();
                var configuredThreshold = alarm.Threshold.CopyWith(value: values.Threshold);

                if (mergedConfig.ThresholdIsAbsolute ?? DynamoResourceConfig.ThresholdIsAbsoluteDefault)
                {
                    configuredThreshold.ThresholdType = ThresholdType.Absolute;
                }

                var threshold = await ThresholdCalculator.ExpandThreshold(_attributeProvider,
                    entity,
                    mergedConfig,
                    configuredThreshold);

                var built = alarm.CopyWith(threshold, values);

                var model = new Alarm
                {
                    AlarmName = $"{resourceConfig.Resource.Name}-{built.Name}-{groupParameters.AlarmNameSuffix}",
                    AlarmDescription = groupParameters.DefaultAlarmDescription(resourceConfig.Definition),
                    ResourceIdentifier = resourceConfig.Resource.Name,
                    Dimensions = dimensions,
                    AlarmDefinition = built
                };

                result.Add(model);
            }

            return result;
        }

        private async Task<IList<Alarm>> BuildIndexAlarms(ResourceAndThresholdsPair<DynamoResourceConfig, TableDescription> resourceConfig,
            PopulatedServiceAlarms<DynamoResourceConfig, TableDescription> service,
            AlertingGroupParameters groupParameters,
            TableDescription parentTableDescription)
        {
            // called twice
            var mergedConfig = service.Options.OverrideWith(resourceConfig.Definition.Options);

            var result = new List<Alarm>();

            var gsiSet = parentTableDescription.GlobalSecondaryIndexes;

            var mergedValuesByAlarmName = service.Values
                .OverrideWith(resourceConfig.Definition.Values);

            var defaults = _defaultAlarms.DynamoDbGsiRead;
            if (mergedConfig.MonitorWrites ?? DynamoResourceConfig.MonitorWritesDefault)
            {
                defaults = defaults.Concat(_defaultAlarms.DynamoDbGsiWrite).ToArray();
            }

            foreach (var gsi in gsiSet)
            {
                foreach (var alarm in defaults)
                {
                    var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();
                    var configuredThreshold = alarm.Threshold.CopyWith(value: values.Threshold);
                    var dimensions = _gsiProvider.GetDimensions(gsi, parentTableDescription, alarm.DimensionNames);

                    if (mergedConfig.ThresholdIsAbsolute ?? DynamoResourceConfig.ThresholdIsAbsoluteDefault)
                    {
                        configuredThreshold.ThresholdType = ThresholdType.Absolute;
                    }

                    var threshold = await ThresholdCalculator.ExpandThreshold(_gsiProvider,
                        gsi,
                        mergedConfig,
                        configuredThreshold);

                    var built = alarm.CopyWith(threshold, values);

                    var model = new Alarm
                    {
                        AlarmName = $"{resourceConfig.Resource.Name}-{gsi.IndexName}-{alarm.Name}-{groupParameters.AlarmNameSuffix}",
                        AlarmDescription = groupParameters.DefaultAlarmDescription(resourceConfig.Definition),
                        ResourceIdentifier = resourceConfig.Resource.Name,
                        Dimensions = dimensions,
                        AlarmDefinition = built
                    };

                    result.Add(model);
                }
            }

            return result;
        }
    }
}
