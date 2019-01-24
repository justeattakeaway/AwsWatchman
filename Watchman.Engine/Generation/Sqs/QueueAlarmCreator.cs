using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.LegacyTracking;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Sqs
{
    public class QueueAlarmCreator : IQueueAlarmCreator
    {
        private readonly IAmazonCloudWatch _cloudWatchClient;
        private readonly IAlarmLogger _logger;
        private readonly IAlarmFinder _alarmFinder;
        private readonly ILegacyAlarmTracker _tracker;

        public QueueAlarmCreator(IAmazonCloudWatch cloudWatchClient,
            IAlarmFinder alarmFinder,
            IAlarmLogger logger, ILegacyAlarmTracker tracker)
        {
            _cloudWatchClient = cloudWatchClient;
            _alarmFinder = alarmFinder;
            _logger = logger;
            _tracker = tracker;
        }

        public int AlarmPutCount { get; private set; }

        public async Task EnsureLengthAlarm(string queueName, int threshold,
            string alarmNameSuffix,
             string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(queueName, AwsMetrics.MessagesVisible, alarmNameSuffix);

            _tracker.Register(alarmName);

            var alarmNeedsUpdate = await InspectExistingAlarm(alarmName,
                threshold, AwsConstants.FiveMinutesInSeconds, snsTopicArn);

            if (!alarmNeedsUpdate)
            {
                return;
            }

            if (dryRun)
            {
                _logger.Info($"Skipped due to dry run: Put queue length alarm {alarmName} at threshold {threshold}");
                return;
            }

            await PutQueueLengthAlarm(alarmName, queueName, snsTopicArn, threshold);
        }

        public async Task EnsureOldestMessageAlarm(string queueName, int threshold, string alarmNameSuffix, string snsTopicArn, bool dryRun)
        {
            var alarmName = GetAlarmName(queueName, AwsMetrics.AgeOfOldestMessage, alarmNameSuffix);

            _tracker.Register(alarmName);

            var alarmNeedsUpdate = await InspectExistingAlarm(alarmName,
                threshold, AwsConstants.FiveMinutesInSeconds, snsTopicArn);

            if (!alarmNeedsUpdate)
            {
                return;
            }

            if (dryRun)
            {
                _logger.Info($"Skipped due to dry run: Put queue oldest message alarm {alarmName} at threshold {threshold}");
                return;
            }

            await PutQueueOldestMessageAlarm(alarmName, queueName, snsTopicArn, threshold);
        }

        private async Task PutQueueLengthAlarm(string alarmName, string queueName,
            string snsTopicArn, double thresholdInUnits)
        {
            var alarmRequest = new PutMetricAlarmRequest
            {
                AlarmName = alarmName,
                AlarmDescription = AwsConstants.DefaultDescription,
                MetricName = AwsMetrics.MessagesVisible,
                Statistic = new Statistic("Sum"),
                Dimensions = new List<Dimension>
                {
                    new Dimension {Name = "QueueName", Value = queueName}
                },
                EvaluationPeriods = 1,
                Period = AwsConstants.FiveMinutesInSeconds,
                Threshold = thresholdInUnits,
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Namespace = AwsNamespace.Sqs,
                AlarmActions = new List<string> { snsTopicArn },
                OKActions = new List<string> { snsTopicArn }
            };

            await _cloudWatchClient.PutMetricAlarmAsync(alarmRequest);

            AlarmPutCount++;
            _logger.Info($"Put queue length alarm {alarmName} at threshold {thresholdInUnits}");
        }

        private async Task PutQueueOldestMessageAlarm(string alarmName, string queueName,
            string snsTopicArn, double thresholdInUnits)
        {
            var alarmRequest = new PutMetricAlarmRequest
            {
                AlarmName = alarmName,
                AlarmDescription = AwsConstants.DefaultDescription,
                MetricName = AwsMetrics.AgeOfOldestMessage,
                Statistic = new Statistic("Maximum"),
                Dimensions = new List<Dimension>
                {
                    new Dimension {Name = "QueueName", Value = queueName}
                },
                EvaluationPeriods = 1,
                Period = AwsConstants.FiveMinutesInSeconds,
                Threshold = thresholdInUnits,
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Namespace = AwsNamespace.Sqs,
                AlarmActions = new List<string> { snsTopicArn },
                OKActions = new List<string> { snsTopicArn }
            };
            await _cloudWatchClient.PutMetricAlarmAsync(alarmRequest);

            AlarmPutCount++;
            _logger.Info($"Put queue oldest message alarm {alarmName} at threshold {thresholdInUnits}");
        }

        private static string GetAlarmName(string queueName, string metricName, string alarmNameSuffix)
        {
            return $"{queueName}-{metricName}-{alarmNameSuffix}";
        }

        private async Task<bool> InspectExistingAlarm(string alarmName,
            double thresholdInUnits, int periodSeconds, string targetTopic)
        {
            var existingAlarm = await _alarmFinder.FindAlarmByName(alarmName);

            if (existingAlarm == null)
            {
                _logger.Info($"Queue alarm {alarmName} does not already exist. Creating it at threshold {thresholdInUnits}");
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
                _logger.Detail($"Queue alarm {alarmName} already exists at same threshold {existingAlarm.Threshold}");
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
                _logger.Info($"Queue alarm {alarmName} already exists at lower threshold {existingAlarm.Threshold}. Will increase to {thresholdInUnits}");
            }
            else
            {
                _logger.Info($"Queue alarm {alarmName} already exists at higher threshold {existingAlarm.Threshold}. Will decrease to {thresholdInUnits}");
            }
        }
    }
}
