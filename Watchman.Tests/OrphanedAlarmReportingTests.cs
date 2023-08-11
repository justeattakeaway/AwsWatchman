using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.LegacyTracking;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests
{
    [TestFixture]
    public class OrphanedAlarmReportingTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task OrphanedAlarmsAreReportedCorrectly(bool configuredAlarmsAlreadyExist)
        {
            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(new WatchmanConfiguration()
                {
                    AlertingGroups = new List<AlertingGroup>()
                    {
                        new AlertingGroup()
                        {
                            Name = "TestAlertingGroup",
                            AlarmNameSuffix = "suffix",
                            Targets = new List<AlertTarget>()
                            {
                                new AlertEmail("test@example.com")
                            },
                            DynamoDb = new DynamoDb()
                            {
                                Tables = new List<Table>()
                                {
                                    new Table()
                                    {
                                        Pattern = "table-with-watchman-alarm"
                                    }
                                }
                            },
                            Sqs = new Configuration.Sqs()
                            {
                                Queues = new List<Queue>()
                                {
                                    new Queue()
                                    {
                                        Pattern = "queue-with-watchman-alarm"
                                    }
                                },
                                Errors = new ErrorQueue()
                                {
                                    Monitored = true
                                }
                            }
                        }
                    }
                });

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new List<TableDescription>()
            {
                new TableDescription()
                {
                    TableName = "table-with-watchman-alarm",
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>()
                    {
                        new GlobalSecondaryIndexDescription()
                        {
                            IndexName = "index",
                            ProvisionedThroughput = new ProvisionedThroughputDescription()
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                }
            });

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new List<string>()
            {
                "queue-with-watchman-alarm",
                "queue-with-watchman-alarm_Error"
            });

            var alarmsPut = new List<string>();

            ioc.GetMock<IAmazonCloudWatch>()
                .PutMetricAlarmAsync(Arg.Do<PutMetricAlarmRequest>(a => alarmsPut.Add(a.AlarmName)), Arg.Any<CancellationToken>())
                .Returns(new PutMetricAlarmResponse());

            var existingWatchmanAlarmsMatchingResources =
                (configuredAlarmsAlreadyExist
                    ? new[]
                    {
                        "table-with-watchman-alarm-ConsumedReadCapacityUnits-suffix",
                        "table-with-watchman-alarm-ReadThrottleEvents-suffix",
                        "table-with-watchman-alarm-index-ConsumedReadCapacityUnits-suffix",
                        "table-with-watchman-alarm-index-ReadThrottleEvents-suffix",
                        "table-with-watchman-alarm-WriteThrottleEvents-suffix",
                        "table-with-watchman-alarm-index-ConsumedWriteCapacityUnits-suffix",
                        "table-with-watchman-alarm-index-WriteThrottleEvents-suffix",
                        "queue-with-watchman-alarm-ApproximateNumberOfMessagesVisible-suffix",
                        "queue-with-watchman-alarm-ApproximateAgeOfOldestMessage-suffix",
                        "queue-with-watchman-alarm_Error-ApproximateNumberOfMessagesVisible-suffix",
                        "queue-with-watchman-alarm_Error-ApproximateAgeOfOldestMessage-suffix"
                    }
                    : new string[0])
                .Select(x => new MetricAlarm()
                {
                    AlarmDescription = "AwsWatchman",
                    AlarmName = x
                })
                .ToArray();

            ioc.GetMock<IAmazonCloudWatch>()
                .DescribeAlarmsAsync(Arg.Any<DescribeAlarmsRequest>(), Arg.Any<CancellationToken>())
                .Returns(new DescribeAlarmsResponse()
                {
                    MetricAlarms = new List<MetricAlarm>()
                    {
                        new MetricAlarm()
                        {
                            AlarmDescription = "AwsWatchman",
                            AlarmName = "orphaned-watchman-alarm"
                        },
                        new MetricAlarm()
                        {
                            AlarmDescription = "",
                            AlarmName = "other-service-alarm"
                        },
                        new MetricAlarm()
                        {
                            AlarmDescription = "AwsWatchman. Alerting Group: test",
                            AlarmName = "newer-cloudformation-alarm"
                        }
                    }.Concat(existingWatchmanAlarmsMatchingResources).ToList()
                });

            ioc.GetMock<IAmazonSimpleNotificationService>()
                .CreateTopicAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new CreateTopicResponse()
                {
                    TopicArn = "topic-arn"
                });

            var generator = ioc.Get<AlarmLoaderAndGenerator>();
            await generator.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            var reporter = ioc.Get<IOrphanedAlarmReporter>();
            var reported = await reporter.FindOrphanedAlarms();

            Assert.That(alarmsPut, Is.Not.Empty);
            Assert.That(reported.Count, Is.EqualTo(1));
            Assert.That(reported[0].AlarmName, Is.EqualTo("orphaned-watchman-alarm"));
        }
    }
}
