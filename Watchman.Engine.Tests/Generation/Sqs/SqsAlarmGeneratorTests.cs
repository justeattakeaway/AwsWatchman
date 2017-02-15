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
    public class SqsAlarmGeneratorTests
    {
        [Test]
        public async Task DryRunShouldCallQueueAlarmCreator()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "queue1" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakeSimpleConfig();

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.DryRun);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "queue1", true);
        }

        [Test]
        public async Task GenerateAlarmsRunShouldCallQueueAlarmCreator()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string> { "queue1" });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakeSimpleConfig();

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "queue1", false);
        }

        [Test]
        public async Task ExtraQueuesInResourcesShouldBeIgnored()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string>
                {
                    "aa_nomatch_queue",
                    "prod-pattern-queue",
                    "prod-pattern-queue-two",
                    "zz_nomatch_queue"
                });

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
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue-two", 10, false);
            VerifyQueues.NoLengthAlarm(alarmCreator, "aa_nomatch_queue");
            VerifyQueues.NoLengthAlarm(alarmCreator, "zz_nomatch_queue");
        }

        [Test]
        public async Task ExtraQueueInPatternShouldBeIgnored()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string>
                {
                    "aa_nomatch_queue",
                    "prod-pattern-queue",
                    "prod-pattern-queue-two",
                    "zz_nomatch_queue"
                });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            config.AlertingGroups[0].Sqs.Queues = new List<Queue>
                {
                    new Queue {Pattern = "nopatternmatch"},
                    new Queue {Pattern = "prod-pattern-queue"}
                };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.NoLengthAlarm(alarmCreator, "nopatternmatch");
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 100, false);
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue-two", 100, false);
        }

        [Test]
        public async Task ExtraQueueInNameShouldBeIgnored()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string>
                {
                    "aa_nomatch_queue",
                    "prod-pattern-queue",
                    "prod-pattern-queue-two",
                    "zz_nomatch_queue"
                });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakePatternConfig();
            config.AlertingGroups[0].Sqs.Queues = new List<Queue>
                {
                    new Queue {Name = "nopatternmatch"},
                    new Queue {Name = "prod-pattern-queue"}
                };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.NoLengthAlarm(alarmCreator, "nopatternmatch");
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 100, false);
            VerifyQueues.NoLengthAlarm(alarmCreator, "prod-pattern-queue-two");
        }


        [Test]
        public async Task PatternQueueShouldAlsoMonitorMatchingErrorQueue()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string>
                {
                    "prod-pattern-queue",
                    "prod-pattern-queue_error",
                    "some-other-queue"
                });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakeSimpleConfig();
            var sqsConfig = config.AlertingGroups[0].Sqs;

            sqsConfig.LengthThreshold = 11;
            sqsConfig.Errors = new ErrorQueue
            {
                Monitored = true,
                LengthThreshold = 7
            };

            sqsConfig.Queues = new List<Queue>
            {
                new Queue
                {
                    Pattern = "^prod-pattern-queue"
                }
            };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 11, false);
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue_error", 7, false);
            VerifyQueues.NoLengthAlarm(alarmCreator, "some-other-queue");
        }


        [Test]
        public async Task NamedQueueShouldAlsoMonitorMatchingErrorQueue()
        {
            var queueSource = new Mock<IResourceSource<QueueData>>();
            VerifyQueues.ReturnsQueues(queueSource, new List<string>
                {
                    "prod-pattern-queue",
                    "prod-pattern-queue_error",
                    "some-other-queue"
                });

            var alarmCreator = new Mock<IQueueAlarmCreator>();
            var snsTopicCreator = new Mock<ISnsTopicCreator>();
            var snsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            var snsCreator = new SnsCreator(snsTopicCreator.Object, snsSubscriptionCreator.Object);

            var config = MakeSimpleConfig();
            var sqsConfig = config.AlertingGroups[0].Sqs;

            sqsConfig.LengthThreshold = 11;
            sqsConfig.Errors = new ErrorQueue
            {
                Monitored = true,
                LengthThreshold = 7
            };

            sqsConfig.Queues = new List<Queue>
            {
                new Queue
                {
                    Name = "prod-pattern-queue"
                }
            };

            var populator = new QueueNamePopulator(new ConsoleAlarmLogger(false),
                queueSource.Object);

            var generator = new SqsAlarmGenerator(
                new ConsoleAlarmLogger(false), queueSource.Object,
                populator, alarmCreator.Object,
                snsCreator);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue", 11, false);
            VerifyQueues.EnsureLengthAlarm(alarmCreator, "prod-pattern-queue_error", 7, false);
            VerifyQueues.NoLengthAlarm(alarmCreator, "some-other-queue");
        }

        private static WatchmanConfiguration MakeSimpleConfig()
        {
            var alertingGroup = new AlertingGroup
            {
                AlarmNameSuffix = "test1",
                Sqs = new Configuration.Sqs
                {
                    Queues = new List<Queue>
                    {
                        new Queue {Name = "queue1"}
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
