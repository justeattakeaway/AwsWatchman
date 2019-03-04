using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.DocumentModel;
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
        private readonly IResourceSource<TableDescription> _tableSource;
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
            _tableSource = tableSource;
            _dimensions = dimensionProvider;
            _attributeProvider = attributeProvider;
            _defaultAlarms = defaultAlarms;
            _logger = logger;
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            AwsServiceAlarms<DynamoResourceConfig> service,
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
            ResourceThresholds<DynamoResourceConfig> resource,
            AwsServiceAlarms<DynamoResourceConfig> service,
            AlertingGroupParameters groupParameters)
        {
            var entity = await _tableSource.GetResourceAsync(resource.Name);

            if (entity == null)
            {
                _logger.Error($"Skipping table {resource.Name} as it does not exist");
                return Array.Empty<Alarm>();
            }

            var result = await BuildTableAlarms(resource, service, groupParameters, entity);

            result.AddRange(await BuildIndexAlarms(resource, service, groupParameters, entity));

            return result;
        }

        private async Task<List<Alarm>> BuildTableAlarms(ResourceThresholds<DynamoResourceConfig> resourceConfig,
            AwsServiceAlarms<DynamoResourceConfig> service,
            AlertingGroupParameters groupParameters,
            AwsResource<TableDescription> entity)
        {
            var mergedConfig = service.Options.OverrideWith(resourceConfig.Options);

            var result = new List<Alarm>();

            var mergedValuesByAlarmName = service.Values.OverrideWith(resourceConfig.Values);

            var defaults = _defaultAlarms.DynamoDbRead;
            if (mergedConfig.MonitorWrites ?? DynamoResourceConfig.MonitorWritesDefault)
            {
                defaults = defaults.Concat(_defaultAlarms.DynamoDbWrite).ToArray();
            }

            foreach (var alarm in defaults)
            {
                var dimensions = _dimensions.GetDimensions(entity.Resource, alarm.DimensionNames);
                var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();
                var configuredThreshold = alarm.Threshold.CopyWith(value: values.Threshold);

                if (mergedConfig.ThresholdIsAbsolute ?? DynamoResourceConfig.ThresholdIsAbsoluteDefault)
                {
                    configuredThreshold.ThresholdType = ThresholdType.Absolute;
                }

                var threshold = await ThresholdCalculator.ExpandThreshold(_attributeProvider,
                    entity.Resource,
                    mergedConfig,
                    configuredThreshold);

                var built = alarm.CopyWith(threshold, values);

                var model = new Alarm
                {
                    AlarmName = $"{resourceConfig.Name}-{built.Name}-{groupParameters.AlarmNameSuffix}",
                    AlarmDescription = groupParameters.DefaultAlarmDescription(),
                    Resource = entity,
                    Dimensions = dimensions,
                    AlarmDefinition = built
                };

                result.Add(model);
            }

            return result;
        }

        private async Task<IList<Alarm>> BuildIndexAlarms(ResourceThresholds<DynamoResourceConfig> resourceConfig,
            AwsServiceAlarms<DynamoResourceConfig> service,
            AlertingGroupParameters groupParameters,
            AwsResource<TableDescription> parentTableEntity)
        {
            // called twice
            var mergedConfig = service.Options.OverrideWith(resourceConfig.Options);

            var result = new List<Alarm>();

            var gsiSet = parentTableEntity.Resource.GlobalSecondaryIndexes;

            var mergedValuesByAlarmName = service.Values.OverrideWith(resourceConfig.Values);

            var defaults = _defaultAlarms.DynamoDbGsiRead;
            if (mergedConfig.MonitorWrites ?? DynamoResourceConfig.MonitorWritesDefault)
            {
                defaults = defaults.Concat(_defaultAlarms.DynamoDbGsiWrite).ToArray();
            }

            foreach (var gsi in gsiSet)
            {
                var gsiResource = new AwsResource<GlobalSecondaryIndexDescription>(gsi.IndexName, gsi);

                foreach (var alarm in defaults)
                {
                    var values = mergedValuesByAlarmName.GetValueOrDefault(alarm.Name) ?? new AlarmValues();
                    var configuredThreshold = alarm.Threshold.CopyWith(value: values.Threshold);
                    var dimensions = _gsiProvider.GetDimensions(gsi, parentTableEntity.Resource, alarm.DimensionNames);

                    if (mergedConfig.ThresholdIsAbsolute ?? DynamoResourceConfig.ThresholdIsAbsoluteDefault)
                    {
                        configuredThreshold.ThresholdType = ThresholdType.Absolute;
                    }

                    var threshold = await ThresholdCalculator.ExpandThreshold(_gsiProvider,
                        gsiResource.Resource,
                        mergedConfig,
                        configuredThreshold);

                    var built = alarm.CopyWith(threshold, values);

                    var model = new Alarm
                    {
                        AlarmName = $"{resourceConfig.Name}-{gsi.IndexName}-{alarm.Name}-{groupParameters.AlarmNameSuffix}",
                        AlarmDescription = groupParameters.DefaultAlarmDescription(),
                        // TODO: remove this property in a future PR
                        // passing in the entity shouldn't be necessary and passing in the table entity here
                        // when the alarm is for a GSI is an even worse hack
                        Resource = parentTableEntity,
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
