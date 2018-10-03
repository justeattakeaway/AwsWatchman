using System.Collections.Generic;
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

            Assert.That(alarmsForQueue.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                    )
                );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
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

            Assert.That(alarmsForQueue.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                    )
                );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
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
                    && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                    )
                );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 1
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
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
                    && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                    )
                );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
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
                    && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                    )
                );

            Assert.That(alarmsForQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            var alarmsForErrorQueue = alarmsByQueue["first-sqs-queue_error"];

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateNumberOfMessagesVisible"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("NumberOfVisibleMessages_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );

            Assert.That(alarmsForErrorQueue.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "ApproximateAgeOfOldestMessage"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("AgeOfOldestMessage_Error")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 600
                        && alarm.Properties["Period"].Value<int>() == 60
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Sqs
                )
            );
        }
    }
}
