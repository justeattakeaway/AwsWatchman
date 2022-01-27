using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.Sqs
{
    public class SqsAlarmTests
    {
        [Test]
        public async Task IgnoresNamedEntitiesThatDoNotExist()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                        {
                            new ResourceThresholds<SqsResourceConfig>()
                            {
                                Name = "non-existent-queue"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "http://sqs.com/first-queue"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert
            Assert.That(cloudformation.StacksDeployed, Is.Zero);
        }

        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue"
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            Assert.That(alarmsByQueue.ContainsKey("first-sqs-queue"), Is.True);
            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            var alarm1 = alarmsForQueue
                .Single(x => x.Properties["MetricName"].Value<string>().Contains("ApproximateNumberOfMessagesVisible"));

            Assert.That(alarm1.Properties["AlarmName"].Value<string>(), Contains.Substring("NumberOfVisibleMessages"));
            Assert.That(alarm1.Properties["AlarmName"].Value<string>(), Contains.Substring("-group-suffix"));
            Assert.That(alarm1.Properties["Threshold"].Value<int>(), Is.EqualTo(100));
            Assert.That(alarm1.Properties["Period"].Value<int>(), Is.EqualTo(60 * 5));
            Assert.That(alarm1.Properties["ComparisonOperator"].Value<string>(), Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(alarm1.Properties["Statistic"].Value<string>(), Is.EqualTo("Maximum"));
            Assert.That(alarm1.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.Sqs));
            Assert.That(alarm1.Properties["TreatMissingData"].Value<string>(), Is.EqualTo(TreatMissingDataConstants.NotBreaching));

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
                )
            );
        }


        [Test]
        public async Task AlarmWithIncludeErrorQueuesFalseWillNotCreateErrorQueueAlarms()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue",
                            Options = new SqsResourceConfig()
                            {
                                IncludeErrorQueues = false
                            },
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            Assert.That(alarmsByQueue.ContainsKey("first-sqs-queue"), Is.True);
            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            Assert.That(
                alarmsForQueue.Exists(
                    alarm => alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible")
            );

            Assert.That(!alarmsByQueue.ContainsKey("first-sqs-queue_error"));
        }


        [Test]
        public async Task AlarmWithManuallySetThresholdRetainsThatValueForTheRelevantAlarm()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue",
                            Values = new Dictionary<string, AlarmValues>()
                            {
                                { "NumberOfVisibleMessages_Error", new AlarmValues(value: 1) }
                            }
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            Assert.That(alarmsByQueue.ContainsKey("first-sqs-queue"), Is.True);
            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["Threshold"].Value<int>() == 100
                )
            );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["Threshold"].Value<int>() == 600
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["Threshold"].Value<int>() == 1
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["Threshold"].Value<int>() == 600
                )
            );
        }

        [Test]
        public async Task AlarmCreatedWithNameParameterMatchesErrorQueueToo()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Name = "first-sqs-queue"
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            Assert.That(alarmsByQueue.ContainsKey("first-sqs-queue"), Is.True);
            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                )
            );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );
        }

        [Test]
        public async Task RegexPatternThatDoesntAllowAnyCharactersAfterQueueNameWillStillAddAlarmsForErrorQueue()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "^first-sqs-queue$"
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            Assert.That(alarmsByQueue.ContainsKey("first-sqs-queue"), Is.True);
            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                )
            );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                )
            );
        }

        [Test]
        public async Task GuessesErrorQueueNameWhenNotReportedByCloudWatch()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue"
                        }
                    }
                }
            });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            var alarmsForQueue = alarmsByQueue["first-sqs-queue"];

            Assert.That(alarmsForQueue, Is.Not.Empty);

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue, Is.Not.Empty);

        }

        [Test]
        public async Task GenerateAlarmsForErrorQueue_WhenWorkingQueueDoesNotExists()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue"
                        }
                    }
                }
            });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue, Is.Not.Empty);

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );
        }

        [Test]
        public async Task GroupDisabledAlarmEnabledWhenThresholdSetForResource()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue",
                            Values = new Dictionary<string, AlarmValues>()
                            {
                                { "AgeOfOldestMessage_Error",  10 }
                            }
                        }
                    },
                    Values = new Dictionary<string, AlarmValues>()
                    {
                        { "AgeOfOldestMessage_Error",  new AlarmValues(enabled: false) }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            var alarm = alarmsForErrorQueue.SingleOrDefault(
                a =>
                    a.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                    && a.Properties["Threshold"].Value<int>() == 10
            );

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.Properties["Threshold"].Value<int>(), Is.EqualTo(10));
        }

        [Test]
        public async Task GroupEnabledAlarmDisabledWhenResourceDisables()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                    {
                        new ResourceThresholds<SqsResourceConfig>()
                        {
                            Pattern = "first-sqs-queue",
                            Values = new Dictionary<string, AlarmValues>()
                            {
                                { "AgeOfOldestMessage_Error",  new AlarmValues(enabled: false) }
                            }
                        }
                    },
                    Values = new Dictionary<string, AlarmValues>()
                    {
                        { "AgeOfOldestMessage_Error",  10 }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByQueue = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("QueueName");

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            var alarm = alarmsForErrorQueue.SingleOrDefault(
                a =>
                    a.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
            );

            Assert.That(alarm, Is.Null);
        }
    }
}
