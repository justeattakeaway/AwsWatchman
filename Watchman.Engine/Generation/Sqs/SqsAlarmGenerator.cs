using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Generation.Sqs
{
    public class SqsAlarmGenerator : ISqsAlarmGenerator
    {
        private readonly IAlarmLogger _logger;
        private readonly IResourceSource<QueueData> _queueSource;
        private readonly QueueNamePopulator _queueNamePopulator;
        private readonly IQueueAlarmCreator _queueAlarmCreator;
        private readonly SnsCreator _snsCreator;

        private static readonly ErrorQueue ErrorQueueDefaults = new ErrorQueue
            {
                Monitored = true,
                Suffix = "_error",
                LengthThreshold = AwsConstants.ErrorQueueLengthThreshold,
                OldestMessageThreshold = null
            };

        public SqsAlarmGenerator(IAlarmLogger logger,
            IResourceSource<QueueData> queueSource,
            QueueNamePopulator queueNamePopulator,
            IQueueAlarmCreator queueAlarmCreator,
            SnsCreator snsCreator)
        {
            _logger = logger;
            _queueSource = queueSource;
            _queueNamePopulator = queueNamePopulator;
            _queueAlarmCreator = queueAlarmCreator;
            _snsCreator = snsCreator;
        }

        public async Task GenerateAlarmsFor(WatchmanConfiguration config, RunMode mode)
        {
            var dryRun = mode == RunMode.DryRun;

            await LogQueueNames();

            foreach (var alertingGroup in config.AlertingGroups)
            {
                await GenerateAlarmsFor(alertingGroup, dryRun);
            }

            ReportPutCounts(dryRun);
        }

        private async Task LogQueueNames()
        {
            var queueNames = await _queueSource.GetResourceNamesAsync();
            if (queueNames == null)
            {
                _logger.Info("No queues found");
                return;
            }

            _logger.Info($"Preloaded all {queueNames.Count} queues");

            foreach (var queueName in queueNames)
            {
                _logger.Detail(queueName);
            }
        }

        private async Task GenerateAlarmsFor(AlertingGroup alertingGroup, bool dryRun)
        {
            if (alertingGroup.Sqs?.Queues == null || alertingGroup.Sqs.Queues.Count == 0)
            {
                return;
            }

            SetErrorDefaults(alertingGroup);

            await _queueNamePopulator.PopulateSqsNames(alertingGroup);

            var snsTopic = await _snsCreator.EnsureSnsTopic(alertingGroup, dryRun);

            var queueResourceNames = await _queueSource.GetResourceNamesAsync();

            await EnsureAllQueueAlarms(alertingGroup, queueResourceNames, snsTopic, dryRun);
        }

        private static void SetErrorDefaults(AlertingGroup alertingGroup)
        {
            SetErrorDefaultsOnAlertingGroup(alertingGroup);
            foreach (var configuredQueue in alertingGroup.Sqs.Queues)
            {
                SetErrorDefaultsOnQueue(alertingGroup, configuredQueue);
            }
        }

        private static void SetErrorDefaultsOnAlertingGroup(AlertingGroup alertingGroup)
        {
            if (alertingGroup.Sqs.Errors == null)
            {
                alertingGroup.Sqs.Errors = new ErrorQueue();
            }
            alertingGroup.Sqs.Errors.ReadDefaults(ErrorQueueDefaults);
        }

        private static void SetErrorDefaultsOnQueue(AlertingGroup group, Queue queue)
        {
            if (queue.Errors == null)
            {
                queue.Errors = new ErrorQueue();
            }
            queue.Errors.ReadDefaults(group.Sqs.Errors);
        }

        private async Task EnsureAllQueueAlarms(AlertingGroup alertingGroup,
            IList<string> queueResourceNames, string snsTopic, bool dryRun)
        {
            var configuredQueues = alertingGroup.Sqs.Queues;

            foreach (var configuredQueue in configuredQueues)
            {
                if (queueResourceNames.Contains(configuredQueue.Name))
                {
                    await EnsureQueueAlarms(alertingGroup, configuredQueue, snsTopic, dryRun);
                }
                else
                {
                    _logger.Info($"No match in active queues for queue {configuredQueue.Name}");
                }
            }
        }

        private async Task EnsureQueueAlarms(AlertingGroup group,
            Queue configuredQueue, string snsTopic, bool dryRun)
        {
            try
            {
                if (configuredQueue.IsErrorQueue() && !configuredQueue.ErrorsMonitored())
                {
                    _logger.Info($"Skipping error queue {configuredQueue.Name}");
                    return;
                }

                await EnsureActiveQueueAlarms(group, configuredQueue, snsTopic, dryRun);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when creating queue alarm for {configuredQueue.Name}");
                throw;
            }
        }

        private async Task EnsureActiveQueueAlarms(
            AlertingGroup group, Queue queue,
            string snsTopic, bool dryRun)
        {
            var lengthThreshold = QueueLengthThreshold(queue, group);

            await _queueAlarmCreator.EnsureLengthAlarm(
                queue.Name, lengthThreshold,
                group.AlarmNameSuffix, snsTopic, dryRun);

            var oldestMessageThreshold = OldestMessageThreshold(queue, group);

            if (oldestMessageThreshold.HasValue && (lengthThreshold > 1))
            {
                await _queueAlarmCreator.EnsureOldestMessageAlarm(
                    queue.Name, oldestMessageThreshold.Value,
                    group.AlarmNameSuffix, snsTopic, dryRun);
            }
        }

        private int QueueLengthThreshold(Queue queue, AlertingGroup group)
        {
            if (queue.IsErrorQueue())
            {
                return queue.Errors.LengthThreshold.Value;
            }

            return queue.LengthThreshold ?? group.Sqs.LengthThreshold ?? AwsConstants.QueueLengthThreshold;
        }

        private int? OldestMessageThreshold(Queue queue, AlertingGroup group)
        {
            if (queue.IsErrorQueue())
            {
                return queue.Errors.OldestMessageThreshold;
            }

            return queue.OldestMessageThreshold ?? group.Sqs.OldestMessageThreshold ?? AwsConstants.OldestMessageThreshold;
        }

        private void ReportPutCounts(bool dryRun)
        {
            if (dryRun)
            {
                if (_queueAlarmCreator.AlarmPutCount > 0)
                {
                    throw new WatchmanException("PUTs happened in dryRun mode");
                }

                _logger.Info("Dry Run: No queue alarms were put");
                return;
            }

            if (_queueAlarmCreator.AlarmPutCount == 0)
            {
                _logger.Info("No queue alarms were put");
            }
            else
            {
                _logger.Info($"Alarms put: {_queueAlarmCreator.AlarmPutCount} queue alarms");
            }
        }
    }
}
