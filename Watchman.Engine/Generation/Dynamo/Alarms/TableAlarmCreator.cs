using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using Watchman.Engine.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Dynamo.Alarms
{
    public class TableAlarmCreator : ITableAlarmCreator
    {
        private readonly IAmazonCloudWatch _cloudWatchClient;
        private readonly IAlarmFinder _alarmFinder;
        private readonly IAlarmLogger _logger;

        public TableAlarmCreator(IAmazonCloudWatch cloudWatchClient,
            IAlarmFinder alarmFinder,
            IAlarmLogger logger)
        {
            _cloudWatchClient = cloudWatchClient;
            _alarmFinder = alarmFinder;
            _logger = logger;
        }

        public int AlarmPutCount { get; private set; }

        public async Task EnsureReadCapacityAlarm(TableDescription table, string alarmNameSuffix, double thresholdFraction,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, AwsMetrics.ConsumedReadCapacity, alarmNameSuffix);
            var thresholdInUnits = AlarmThresholds.Calulate(table.ProvisionedThroughput.ReadCapacityUnits, thresholdFraction);

            await CheckTableAlarm(alarmName, table.TableName, AwsMetrics.ConsumedReadCapacity,
                thresholdInUnits, AwsConstants.FiveMinutesInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureReadThrottleAlarm(TableDescription table, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, AwsMetrics.ReadThrottleEvents, alarmNameSuffix);

            await CheckTableAlarm(alarmName, table.TableName, AwsMetrics.ReadThrottleEvents,
                threshold, AwsConstants.OneMinuteInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureWriteCapacityAlarm(TableDescription table, string alarmNameSuffix, double thresholdFraction,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, AwsMetrics.ConsumedWriteCapacity, alarmNameSuffix);
            var thresholdInUnits = AlarmThresholds.Calulate(table.ProvisionedThroughput.WriteCapacityUnits, thresholdFraction);

            await CheckTableAlarm(alarmName, table.TableName, AwsMetrics.ConsumedWriteCapacity,
                thresholdInUnits, AwsConstants.FiveMinutesInSeconds, snsTopicArn, dryRun);
        }

        public async Task EnsureWriteThrottleAlarm(TableDescription table, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(table, AwsMetrics.WriteThrottleEvents, alarmNameSuffix);

            await CheckTableAlarm(alarmName, table.TableName, AwsMetrics.WriteThrottleEvents,
                threshold, AwsConstants.OneMinuteInSeconds, snsTopicArn, dryRun);
        }

        private static string GetAlarmName(TableDescription table, string metricName, string alarmNameSuffix)
        {
            return $"{table.TableName}-{metricName}-{alarmNameSuffix}";
        }

        private async Task CheckTableAlarm(string alarmName, string tableName, string metricName,
            double thresholdInUnits, int periodSeconds,
            string snsTopicArn, bool dryRun)
        {
            var alarmNeedsUpdate = await InspectExistingAlarm(alarmName, thresholdInUnits, periodSeconds);

            if (!alarmNeedsUpdate)
            {
                return;
            }

            if (dryRun)
            {
                _logger.Info($"Skipped due to dry run: Put table alarm {alarmName} at threshold {thresholdInUnits}");
                return;
            }

            await PutTableAlarm(alarmName, tableName, metricName, snsTopicArn,
                thresholdInUnits, periodSeconds);
        }

        private async Task PutTableAlarm(string alarmName, string tableName, string metricName,
            string snsTopicArn, double thresholdInUnits, int periodSeconds)
        {
            var alarmRequest = new PutMetricAlarmRequest
            {
                AlarmName = alarmName,
                MetricName = metricName,
                Statistic = new Statistic("Sum"),
                Dimensions = new List<Dimension>
                {
                    new Dimension {Name = "TableName", Value = tableName}
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
            _logger.Info($"Put table alarm {alarmName} at threshold {thresholdInUnits} and period {periodSeconds}s");
        }

        private async Task<bool> InspectExistingAlarm(string alarmName, double thresholdInUnits, int periodSeconds)
        {
            var existingAlarm = await _alarmFinder.FindAlarmByName(alarmName);

            if (existingAlarm == null)
            {
                _logger.Info($"Table alarm {alarmName} does not already exist. Creating it at threshold {thresholdInUnits}");
                return true;
            }

            if (AlarmThresholds.AreEqual(existingAlarm.Threshold, thresholdInUnits) && (existingAlarm.Period == periodSeconds))
            {
                _logger.Detail($"Table alarm {alarmName} already exists at same threshold {existingAlarm.Threshold}");
                return false;
            }

            LogDifferences(existingAlarm, alarmName, thresholdInUnits, periodSeconds);
            return true;
        }

        private void LogDifferences(MetricAlarm existingAlarm, string alarmName, double thresholdInUnits, int periodSeconds)
        {
            if (existingAlarm.Period != periodSeconds)
            {
                _logger.Info($"Table alarm {alarmName} period has changed from {existingAlarm.Period} to {periodSeconds}");
            }

            if (AlarmThresholds.AreEqual(existingAlarm.Threshold, thresholdInUnits))
            {
                return;
            }

            if (existingAlarm.Threshold < thresholdInUnits)
            {
                _logger.Info($"Table alarm {alarmName} already exists at lower threshold {existingAlarm.Threshold}. Will increase to {thresholdInUnits}");
            }
            else
            {
                _logger.Info($"Table alarm {alarmName} already exists at higher threshold {existingAlarm.Threshold}. Will decrease to {thresholdInUnits}");
            }
        }
    }
}
