using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Tests.Generation.Sqs
{
    [TestFixture]
    public class SqsErrorQueueAlarmGeneratorTests
    {
        [Test]
        public async Task MainAndErrorQueuesAreMonitoredAtDifferentThresholds()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "prod-pattern-queue", "prod-pattern-queue_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);


            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 10, false);
            VerifyQueues.EnsureOldestMessageAlarm(alarmCreator, "prod-pattern-queue",600, false);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue_error", 1, false);
            VerifyQueues.NoOldestMessageAlarm(alarmCreator, "prod-pattern-queue_error");
        }

        [Test]
        public async Task AlertingGroupThresholdsAreUsedByDefault()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "prod-pattern-queue", "prod-pattern-queue_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            var sqs = config.AlertingGroups[0].Sqs;
            sqs.LengthThreshold = 32;
            sqs.OldestMessageThreshold = 17;

            sqs.Queues[0].LengthThreshold = null;
            sqs.Queues[0].OldestMessageThreshold = null;

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);


            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 32, false);
            VerifyQueues.EnsureOldestMessageAlarm(alarmCreator, "prod-pattern-queue", 17, false);
        }

        [Test]
        public async Task AlertingGroupValuesAreUsedByDefault()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "pattern1", "pattern1_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            var sqs = config.AlertingGroups[0].Sqs;

            sqs.Errors = new ErrorQueue
                {
                    LengthThreshold = 42,
                    OldestMessageThreshold = 12
                };
            sqs.Queues[0].Errors = null;

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "pattern1", 10, false);
            VerifyQueues.EnsureOldestMessageAlarm(alarmCreator, "pattern1", 600, false);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "pattern1_error", 42, false);
            VerifyQueues.EnsureOldestMessageAlarm(alarmCreator, "pattern1_error", 12, false);
        }

        [Test]
        public async Task QueueValuesAreUsedAsOverride()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "pattern1", "pattern1_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            var sqs = config.AlertingGroups[0].Sqs;

            sqs.Errors = new ErrorQueue
            {
                LengthThreshold = 142,
                OldestMessageThreshold = 112
            };
            sqs.Queues[0].LengthThreshold = 42;
            sqs.Queues[0].OldestMessageThreshold = 12;

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "pattern1", 42, false);
            VerifyQueues.EnsureOldestMessageAlarm(alarmCreator, "pattern1", 12, false);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "pattern1_error", 1, false);
            VerifyQueues.NoOldestMessageAlarm(alarmCreator, "pattern1_error");
        }

        [Test]
        public async Task AlertingGroupCanTurnOffErrorQueueMonitoring()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "pattern1", "pattern1_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            var sqs = config.AlertingGroups[0].Sqs;

            sqs.Errors = new ErrorQueue
            {
                Monitored = false
            };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.NoLengthAlarm(alarmCreator, "pattern1_error");
            VerifyQueues.NoOldestMessageAlarm(alarmCreator, "pattern1_error");
        }

        [Test]
        public async Task QueueCanTurnOffErrorQueueMonitoring()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "pattern1", "pattern1_error" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            var sqs = config.AlertingGroups[0].Sqs;

            sqs.Errors = new ErrorQueue
                {
                    Monitored = true
                };
            sqs.Queues[0].Errors = new ErrorQueue
                {
                    Monitored = false
                };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.NoLengthAlarm(alarmCreator, "pattern1_error");
            VerifyQueues.NoOldestMessageAlarm(alarmCreator, "pattern1_error");
        }

        private static WatchmanConfiguration MakePatternConfig()
        {
            var alertingGroup = new AlertingGroup
            {
                AlarmNameSuffix = "test1",
                Sqs = new Configuration.Sqs
                {
                    Queues = new List<Queue>
                    {
                        new Queue
                        {
                            Pattern = "pattern",
                            LengthThreshold = 10,
                            Errors = new ErrorQueue
                            {
                                LengthThreshold = 1
                            }
                        }
                    }
                }
            };

            return new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup>
                {
                    alertingGroup
                }
            };
        }
    }
}
