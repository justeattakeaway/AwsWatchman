using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Generation.Dynamo
{
    public class DynamoResourceAlarmGenerator : IResourceAlarmGenerator<ResourceConfig>
    {
        private readonly IResourceSource<TableDescription> _tableSource;
        private readonly IAlarmDimensionProvider<TableDescription> _dimensions;
        private readonly IAlarmDimensionProvider<GlobalSecondaryIndexDescription> _gsiDimensionProvider = new DynamoDbGsiDataProvider();
        private readonly AlarmBuilder<TableDescription, ResourceConfig> _builder;
        private readonly AlarmBuilder<GlobalSecondaryIndexDescription, ResourceConfig> _gsiAlarmBuilder = new AlarmBuilder<GlobalSecondaryIndexDescription, ResourceConfig>(new DynamoDbGsiDataProvider());

        public DynamoResourceAlarmGenerator(
            IResourceSource<TableDescription> tableSource,
            IAlarmDimensionProvider<TableDescription> dimensionProvider,
            IResourceAttributesProvider<TableDescription, ResourceConfig> attributeProvider)
        {
            _tableSource = tableSource;
            _dimensions = dimensionProvider;
            _builder = new AlarmBuilder<TableDescription, ResourceConfig>(attributeProvider);
        }

        public async Task<IList<Alarm>>  GenerateAlarmsFor(
            AwsServiceAlarms<ResourceConfig> service,
            IList<AlarmDefinition> defaults,
            string alarmSuffix)
        {
            if (service?.Resources == null || service.Resources.Count == 0)
            {
                return new List<Alarm>();
            }
            
            List<Alarm> alarms = new List<Alarm>();

            foreach (var resource in service.Resources)
            {
                var alarmsForResource = await CreateAlarmsForResource(defaults, resource, service, alarmSuffix);
                alarms.AddRange(alarmsForResource);                    
            }

            return alarms;
        }

        private async Task<IList<Alarm>> CreateAlarmsForResource(
            IList<AlarmDefinition> defaults,
            ResourceThresholds<ResourceConfig> resource,
            AwsServiceAlarms<ResourceConfig> service,
            string groupSuffix)
        {
            var entity = await _tableSource.GetResourceAsync(resource.Name);

            if (entity == null)
            {
                throw new Exception($"Entity {resource.Name} not found");
            }

            var result = await BuildTableAlarms(defaults, resource, service, groupSuffix, entity);
            
            result.AddRange(await BuildIndexAlarms(resource, service, groupSuffix, entity));

            return result;
        }

        private async Task<List<Alarm>> BuildTableAlarms(IList<AlarmDefinition> defaults, ResourceThresholds<ResourceConfig> resource, AwsServiceAlarms<ResourceConfig> service, string groupSuffix,
            AwsResource<TableDescription> entity)
        {
            var expanded = await _builder.CopyAndUpdateDefaultAlarmsForResource(entity, defaults, service, resource);

            var result = new List<Alarm>();

            foreach (var alarm in expanded)
            {
                var dimensions = _dimensions.GetDimensions(entity.Resource, alarm.DimensionNames);

                var model = new Alarm
                {
                    AlarmName = $"{resource.Name}-{alarm.Name}-{groupSuffix}",
                    Resource = entity,
                    Dimensions = dimensions,
                    AlarmDefinition = alarm
                };
                result.Add(model);
            }
            return result;
        }

        private async Task<IList<Alarm>> BuildIndexAlarms(ResourceThresholds<ResourceConfig> resource, AwsServiceAlarms<ResourceConfig> service, string groupSuffix,
            AwsResource<TableDescription> entity)
        {
            var result = new List<Alarm>();

            var gsiSet = entity.Resource.GlobalSecondaryIndexes;


            foreach (var gsi in gsiSet)
            {
                var gsiResource = new AwsResource<GlobalSecondaryIndexDescription>(gsi.IndexName, gsi);


                var expandedGsi = await _gsiAlarmBuilder.CopyAndUpdateDefaultAlarmsForResource(
                    gsiResource,
                    Defaults.DynamoDbGsi, service, resource);

                foreach (var gsiAlarm in expandedGsi)
                {
                    var dimensions = _gsiDimensionProvider.GetDimensions(gsi, gsiAlarm.DimensionNames);

                    var model = new Alarm
                    {
                        AlarmName = $"{resource.Name}-{gsi.IndexName}-{gsiAlarm.Name}-{groupSuffix}",
                        Resource = entity,
                        Dimensions = dimensions,
                        AlarmDefinition = gsiAlarm
                    };

                    result.Add(model);
                }
            }

            return result;
        }
    }
}
