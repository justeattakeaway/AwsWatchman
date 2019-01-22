using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Generation.Dynamo
{
    public class DynamoAlarmGenerator : IDynamoAlarmGenerator
    {
        private readonly IAlarmLogger _logger;
        private readonly TableNamePopulator _populator;
        private readonly ITableAlarmCreator _tableAlarmCreator;
        private readonly IIndexAlarmCreator _indexAlarmCreator;

        private readonly SnsCreator _snsCreator;
        private readonly IResourceSource<TableDescription> _tableSource;

        public DynamoAlarmGenerator(
            IAlarmLogger logger,
            TableNamePopulator populator,
            ITableAlarmCreator tableAlarmCreator,
            IIndexAlarmCreator indexAlarmCreator,
            SnsCreator snsCreator,
            IResourceSource<TableDescription> tableSource)
        {
            _logger = logger;
            _populator = populator;
            _tableAlarmCreator = tableAlarmCreator;
            _indexAlarmCreator = indexAlarmCreator;
            _snsCreator = snsCreator;
            _tableSource = tableSource;
        }

        public async Task GenerateAlarmsFor(WatchmanConfiguration config, RunMode mode)
        {
            var dryRun = mode == RunMode.DryRun;

            await LogTableNames();

            foreach (var alertingGroup in config.AlertingGroups)
            {
                await GenerateAlarmsFor(alertingGroup, dryRun);
            }

            ReportPutCounts(dryRun);
        }

        private async Task LogTableNames()
        {
            var tableNames = await _tableSource.GetResourceNamesAsync();
            if (tableNames == null)
            {
                _logger.Info("No tables found");
                return;
            }

            _logger.Info($"Preloaded all {tableNames.Count} tables");

            foreach (var tableName in tableNames)
            {
                _logger.Detail(tableName);
            }
        }

        private async Task GenerateAlarmsFor(AlertingGroup alertingGroup, bool dryRun)
        {
            if (alertingGroup.DynamoDb?.Tables == null || alertingGroup.DynamoDb.Tables.Count == 0)
            {
                return;
            }

            await _populator.PopulateDynamoTableNames(alertingGroup);

            var snsTopic = await _snsCreator.EnsureSnsTopic(alertingGroup, dryRun);

            var readAlarms = AlarmTablesHelper.FilterForRead(alertingGroup);
            readAlarms.SnsTopicArn = snsTopic;
            readAlarms.DryRun = dryRun;
            readAlarms.ThrottlingThreshold = alertingGroup.DynamoDb.ThrottlingThreshold ?? AwsConstants.ThrottlingThreshold;

            await EnsureReadAlarms(readAlarms);

            var writeAlarms = AlarmTablesHelper.FilterForWrite(alertingGroup);
            writeAlarms.SnsTopicArn = snsTopic;
            writeAlarms.DryRun = dryRun;
            writeAlarms.ThrottlingThreshold = alertingGroup.DynamoDb.ThrottlingThreshold ?? AwsConstants.ThrottlingThreshold;

            await EnsureWriteAlarms(writeAlarms);
        }

        private async Task EnsureReadAlarms(AlarmTables alarmTables)
        {
            foreach (var table in alarmTables.Tables)
            {
                await EnsureReadAlarms(alarmTables, table);
            }
        }

        private async Task EnsureReadAlarms(AlarmTables alarmTables, Table table)
        {
            try
            {
                var tableResource = await _tableSource.GetResourceAsync(table.Name);

                if (tableResource == null)
                {
                    _logger.Error($"Skipping table {table.Name} as it does not exist");
                    return;
                }

                var tableDescription = tableResource.Resource;
                var threshold = table.Threshold ?? alarmTables.Threshold;

                await _tableAlarmCreator.EnsureReadCapacityAlarm(tableDescription, alarmTables.AlarmNameSuffix,
                    threshold, alarmTables.SnsTopicArn, alarmTables.DryRun);

                var monitorThrottling = table.MonitorThrottling ?? alarmTables.MonitorThrottling;
                var throttlingThreshold = table.ThrottlingThreshold ?? alarmTables.ThrottlingThreshold;

                if (monitorThrottling)
                {
                    await _tableAlarmCreator.EnsureReadThrottleAlarm(tableDescription, alarmTables.AlarmNameSuffix,
                        throttlingThreshold,
                        alarmTables.SnsTopicArn, alarmTables.DryRun);
                }

                foreach (var index in tableDescription.GlobalSecondaryIndexes)
                {
                    await _indexAlarmCreator.EnsureReadCapacityAlarm(tableDescription, index, alarmTables.AlarmNameSuffix, threshold,
                       alarmTables.SnsTopicArn, alarmTables.DryRun);

                    if (monitorThrottling)
                    {
                        await _indexAlarmCreator.EnsureReadThrottleAlarm(tableDescription, index, alarmTables.AlarmNameSuffix,
                            throttlingThreshold, alarmTables.SnsTopicArn, alarmTables.DryRun);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when creating table read alarm for {table.Name}");
                throw;
            }
        }


        private async Task EnsureWriteAlarms(AlarmTables alarmTables)
        {
            foreach (var table in alarmTables.Tables)
            {
                var monitorWrites = table.MonitorWrites ?? true;
                if (monitorWrites)
                {
                    await EnsureWriteAlarm(alarmTables, table);
                }
                else
                {
                    _logger.Detail($"Not monitoring writes to {table.Name}");
                }
            }
        }

        private async Task EnsureWriteAlarm(AlarmTables alarmTables, Table table)
        {
            try
            {
                var tableResource = await _tableSource.GetResourceAsync(table.Name);

                if (tableResource == null)
                {
                    _logger.Error($"Skipping table {table.Name} as it does not exist");
                    return;
                }

                var tableDescription = tableResource.Resource;
                var threshold = table.Threshold ?? alarmTables.Threshold;

                await _tableAlarmCreator.EnsureWriteCapacityAlarm(tableDescription, alarmTables.AlarmNameSuffix,
                    threshold, alarmTables.SnsTopicArn, alarmTables.DryRun);

                var monitorThrottling = table.MonitorThrottling ?? alarmTables.MonitorThrottling;
                var throttlingThreshold = table.ThrottlingThreshold ?? alarmTables.ThrottlingThreshold;

                if (monitorThrottling)
                {
                    await _tableAlarmCreator.EnsureWriteThrottleAlarm(tableDescription, alarmTables.AlarmNameSuffix,
                        throttlingThreshold,
                        alarmTables.SnsTopicArn, alarmTables.DryRun);
                }

                foreach (var index in tableDescription.GlobalSecondaryIndexes)
                {
                    await _indexAlarmCreator.EnsureWriteCapacityAlarm(tableDescription, index, alarmTables.AlarmNameSuffix, threshold,
                        alarmTables.SnsTopicArn, alarmTables.DryRun);

                    if (monitorThrottling)
                    {
                        await _indexAlarmCreator.EnsureWriteThrottleAlarm(tableDescription, index, alarmTables.AlarmNameSuffix,
                         throttlingThreshold, alarmTables.SnsTopicArn, alarmTables.DryRun);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when creating table write alarm for {table.Name}");
                throw;
            }
        }

        private void ReportPutCounts(bool dryRun)
        {
            if (dryRun)
            {
                if ((_tableAlarmCreator.AlarmPutCount > 0) || (_indexAlarmCreator.AlarmPutCount > 0))
                {
                    throw new WatchmanException("PUTs happened in dryRun mode");
                }

                _logger.Info("Dry Run: No table or index alarms were put");
                return;
            }

            if ((_tableAlarmCreator.AlarmPutCount == 0) && (_indexAlarmCreator.AlarmPutCount == 0))
            {
                _logger.Info("No table or index alarms were put");
            }
            else
            {
                _logger.Info($"Alarms put: {_tableAlarmCreator.AlarmPutCount} table alarms and {_indexAlarmCreator.AlarmPutCount} index alarms");
            }
        }
    }
}
