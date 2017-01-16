using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using Watchman.Engine.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Dynamo.Alarms
{
    public class IndexAlarmCreator : IIndexAlarmCreator
    {
        private readonly IAmazonCloudWatch _cloudWatchClient;
        private readonly IAlarmFinder _alarmFinder;
        private readonly IAlarmLogger _logger;

        public IndexAlarmCreator(IAmazonCloudWatch cloudWatchClient,
            IAlarmFinder alarmFinder,
            IAlarmLogger logger)
        {
            _cloudWatchClient = cloudWatchClient;
            _alarmFinder = alarmFinder;
            _logger = logger;
        }

        public int AlarmPutCount { get; private set; }

        public async Task EnsureReadCapacityAlarm(TableDescription table, GlobalSecondaryIndexDescription index, string alarmNameSuffix,
            double thresholdFraction, string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, index, AwsMetrics.ConsumedReadCapacity, alarmNameSuffix);
            var thresholdInUnits = AlarmThresholds.Calulate(index.ProvisionedThroughput.ReadCapacityUnits, thresholdFraction);

            await CheckIndexAlarm(alarmName, table.TableName, index.IndexName, AwsMetrics.ConsumedReadCapacity,
                thresholdInUnits, AwsConstants.FiveMinutesInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureReadThrottleAlarm(TableDescription table, GlobalSecondaryIndexDescription index, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, index, AwsMetrics.ReadThrottleEvents, alarmNameSuffix);

            await CheckIndexAlarm(alarmName, table.TableName, index.IndexName, AwsMetrics.ReadThrottleEvents,
                threshold, AwsConstants.OneMinuteInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureWriteCapacityAlarm(TableDescription table, GlobalSecondaryIndexDescription index,
            string alarmNameSuffix, double thresholdFraction,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, index, AwsMetrics.ConsumedWriteCapacity, alarmNameSuffix);
            var thresholdInUnits = AlarmThresholds.Calulate(index.ProvisionedThroughput.WriteCapacityUnits, thresholdFraction);

            await CheckIndexAlarm(alarmName, table.TableName, index.IndexName, AwsMetrics.ConsumedWriteCapacity,
                thresholdInUnits, AwsConstants.FiveMinutesInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureWriteThrottleAlarm(TableDescription table, GlobalSecondaryIndexDescription index, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, index, AwsMetrics.WriteThrottleEvents, alarmNameSuffix);

            await CheckIndexAlarm(alarmName, table.TableName, index.IndexName, AwsMetrics.WriteThrottleEvents,
                 threshold, AwsConstants.OneMinuteInSeconds, snsTopicArn, dryRun);
        }

        private static string GetAlarmName(TableDescription table, GlobalSecondaryIndexDescription index, string metricName, string alarmNameSuffix)
        {
            return $"{table.TableName}-{index.IndexName}-{metricName}-{alarmNameSuffix}";
        }

        private async Task CheckIndexAlarm(string alarmName, string tableName, string indexName,
            string metricName, double thresholdInUnits, int periodSeconds,
            string snsTopicArn, bool dryRun)
        {
            var alarmNeedsUpdate = await InspectExistingAlarm(alarmName,
                thresholdInUnits, periodSeconds, snsTopicArn);

            if (!alarmNeedsUpdate)
            {
                return;
            }

            if (dryRun)
            {
                _logger.Info($"Skipped due to dry run: Put index alarm {alarmName} at threshold {thresholdInUnits}");
                return;
            }

            await PutIndexAlarm(alarmName, tableName, indexName, metricName, snsTopicArn,
                thresholdInUnits, periodSeconds);
        }

        private async Task PutIndexAlarm(string alarmName, string tableName, string indexName, string metricName,
            string snsTopicArn, double thresholdInUnits, int periodSeconds)
        {
            var alarmRequest = new PutMetricAlarmRequest
            {
                AlarmName = alarmName,
                MetricName = metricName,
                Statistic = new Statistic("Sum"),
                Dimensions = new List<Dimension>
                {
                    new Dimension {Name = "TableName", Value = tableName},
                    new Dimension {Name = "GlobalSecondaryIndexName", Value = indexName}
                },
                EvaluationPeriods = 1,
                Period = periodSeconds,
                Threshold = thresholdInUnits,
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Namespace = AwsNamespace.DynamoDb,
                AlarmActions = new List<string> {snsTopicArn},
                OKActions = new List<string> { snsTopicArn }
            };

            await _cloudWatchClient.PutMetricAlarmAsync(alarmRequest);
            AlarmPutCount++;
            _logger.Info($"Put index alarm {alarmName} at threshold {thresholdInUnits}");
        }

        private async Task<bool> InspectExistingAlarm(string alarmName,
            double thresholdInUnits, int periodSeconds, string targetTopic)
        {
            var existingAlarm = await _alarmFinder.FindAlarmByName(alarmName);

            if (existingAlarm == null)
            {
                _logger.Info($"Index alarm {alarmName} does not already exist. Creating it at threshold {thresholdInUnits}");
                return true;
            }

            if (!MetricAlarmHelper.AlarmActionsEqualsTarget(existingAlarm.AlarmActions, targetTopic))
            {
                _logger.Info($"Index alarm {alarmName} alarm target has changed to {targetTopic}");
                return true;
            }

            if (!MetricAlarmHelper.AlarmAndOkActionsAreEqual(existingAlarm))
            {
                _logger.Info($"Index alarm {alarmName} alarm actions does not match ok actions");
                return true;
            }

            if (AlarmThresholds.AreEqual(existingAlarm.Threshold, thresholdInUnits) && (existingAlarm.Period == periodSeconds))
            {
                _logger.Detail($"Index alarm {alarmName} already exists at same threshold {existingAlarm.Threshold}");
                return false;
            }

            LogDifferences(existingAlarm, alarmName, thresholdInUnits, periodSeconds);

            return true;
        }

        private void LogDifferences(MetricAlarm existingAlarm, string alarmName, double thresholdInUnits, int periodSeconds)
        {
            if (existingAlarm.Period != periodSeconds)
            {
                _logger.Info($"Index alarm {alarmName} period has changed from {existingAlarm.Period} to {periodSeconds}");
            }

            if (AlarmThresholds.AreEqual(existingAlarm.Threshold, thresholdInUnits))
            {
                return;
            }

            if (existingAlarm.Threshold < thresholdInUnits)
            {
                _logger.Info($"Index alarm {alarmName} already exists at lower threshold {existingAlarm.Threshold}. Will increase to {thresholdInUnits}");
            }
            else
            {
                _logger.Info($"Index alarm {alarmName} already exists at higher threshold {existingAlarm.Threshold}. Will decrease to {thresholdInUnits}");
            }
        }
    }
}
