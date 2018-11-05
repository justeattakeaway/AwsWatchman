using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Configuration.Load;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.Dynamo
{
    public class DynamoAlarmTests
    {
        private const int OneMinuteInSeconds = 60;

        [Test]
        public async Task IgnoresNamedEntitiesThatDoNotExist()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Name = "non-existant-table"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "first-dynamo-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 10,
                        WriteCapacityUnits = 10
                    }
                }
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
                DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>()
                    {
                        new ResourceThresholds<ResourceConfig>()
                        {
                            Name = "first-dynamo-table"
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "first-dynamo-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 100,
                        WriteCapacityUnits = 200
                    }
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            const decimal defaultCapacityThreshold = 0.8m;
            const decimal capacityMultiplier = defaultCapacityThreshold * OneMinuteInSeconds;
            const int defaultThrottleThreshold = 2;

            var alarmsByTable = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("TableName");

            Assert.That(alarmsByTable.ContainsKey("first-dynamo-table"), Is.True);
            var alarms = alarmsByTable["first-dynamo-table"];

            Assert.That(alarms.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "ConsumedReadCapacityUnits"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("ConsumedReadCapacityUnitsHigh")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 100 * capacityMultiplier
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.DynamoDb
                    )
                );

            Assert.That(alarms.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "ConsumedWriteCapacityUnits"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("ConsumedWriteCapacityUnitsHigh")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 200 * capacityMultiplier
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.DynamoDb
                )
            );

            var readThrottleAlarm =
                alarms.Single(a => a.Properties["MetricName"].Value<string>() == "ReadThrottleEvents");

            Assert.That(readThrottleAlarm.Properties["AlarmName"].Value<string>(),
                Contains.Substring("ReadThrottleEventsHigh"));
            Assert.That(readThrottleAlarm.Properties["AlarmName"].Value<string>(), Contains.Substring("-group-suffix"));
            Assert.That(readThrottleAlarm.Properties["Threshold"].Value<int>(), Is.EqualTo(defaultThrottleThreshold));
            Assert.That(readThrottleAlarm.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(readThrottleAlarm.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(readThrottleAlarm.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(readThrottleAlarm.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));

            var writeThrottleAlarm =
                alarms.Single(a => a.Properties["MetricName"].Value<string>() == "WriteThrottleEvents");

            Assert.That(writeThrottleAlarm.Properties["AlarmName"].Value<string>(),
                Contains.Substring("WriteThrottleEventsHigh"));
            Assert.That(writeThrottleAlarm.Properties["AlarmName"].Value<string>(),
                Contains.Substring("-group-suffix"));
            Assert.That(writeThrottleAlarm.Properties["Threshold"].Value<int>(), Is.EqualTo(defaultThrottleThreshold));
            Assert.That(writeThrottleAlarm.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(writeThrottleAlarm.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(writeThrottleAlarm.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(writeThrottleAlarm.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));
        }

        [Test]
        public async Task AlarmsCanBeAddedToGlobalSecondaryIndexes()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                {
                    Resources =
                        new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Name = "first-table"
                            }
                        }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "first-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 100,
                        WriteCapacityUnits = 200
                    },
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>()
                    {
                        new GlobalSecondaryIndexDescription()
                        {
                             IndexName = "first-gsi",
                             ProvisionedThroughput = new ProvisionedThroughputDescription()
                             {
                                 ReadCapacityUnits = 400,
                                 WriteCapacityUnits = 500
                             }
                        }
                    }
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert
            const decimal defaultCapacityThreshold = 0.8m;
            const decimal capacityMultiplier = defaultCapacityThreshold * OneMinuteInSeconds;
            const int defaultThrottleThreshold = 2;

            // basic check we still have table alarms
            Assert.That(cloudformation.Stack("Watchman-test").AlarmsByDimension("TableName").Any(), Is.True);

            var alarmsByGsi = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("GlobalSecondaryIndexName");

            Assert.That(alarmsByGsi.ContainsKey("first-gsi"), Is.True);
            var alarms = alarmsByGsi["first-gsi"];

            var consumedRead = alarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-first-gsi-GsiConsumedReadCapacityUnitsHigh"));
            Assert.That(consumedRead, Is.Not.Null, "GSI read alarm missing");
            Assert.That(consumedRead.Properties["MetricName"].Value<string>(), Is.EqualTo("ConsumedReadCapacityUnits"));
            Assert.That(consumedRead.Properties["Threshold"].Value<int>(),
                Is.EqualTo(400 * capacityMultiplier));
            Assert.That(consumedRead.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(consumedRead.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(consumedRead.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(consumedRead.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));
            Assert.That(consumedRead.Dimension("TableName"), Is.EqualTo("first-table"));

            var consumedWrite = alarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-first-gsi-GsiConsumedWriteCapacityUnitsHigh"));
            Assert.That(consumedWrite, Is.Not.Null, "GSI write alarm missing");
            Assert.That(consumedWrite.Properties["MetricName"].Value<string>(),
                Is.EqualTo("ConsumedWriteCapacityUnits"));
            Assert.That(consumedWrite.Properties["Threshold"].Value<int>(),
                Is.EqualTo(500 * capacityMultiplier));
            Assert.That(consumedWrite.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(consumedWrite.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(consumedWrite.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(consumedWrite.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));
            Assert.That(consumedWrite.Dimension("TableName"), Is.EqualTo("first-table"));

            var readThrottle = alarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-first-gsi-GsiReadThrottleEventsHigh"));
            Assert.That(readThrottle, Is.Not.Null, "GSI read throttle alarm missing");
            Assert.That(readThrottle.Properties["MetricName"].Value<string>(), Is.EqualTo("ReadThrottleEvents"));
            Assert.That(readThrottle.Properties["Threshold"].Value<int>(), Is.EqualTo(defaultThrottleThreshold));
            Assert.That(readThrottle.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(readThrottle.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(readThrottle.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(readThrottle.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));
            Assert.That(readThrottle.Dimension("TableName"), Is.EqualTo("first-table"));

            var writeThrottle = alarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-first-gsi-GsiWriteThrottleEventsHigh"));
            Assert.That(writeThrottle, Is.Not.Null, "GSI write throttle alarm missing");
            Assert.That(writeThrottle.Properties["MetricName"].Value<string>(), Is.EqualTo("WriteThrottleEvents"));
            Assert.That(writeThrottle.Properties["Threshold"].Value<int>(), Is.EqualTo(defaultThrottleThreshold));
            Assert.That(writeThrottle.Properties["Period"].Value<int>(), Is.EqualTo(60));
            Assert.That(writeThrottle.Properties["ComparisonOperator"].Value<string>(),
                Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(writeThrottle.Properties["Statistic"].Value<string>(), Is.EqualTo("Sum"));
            Assert.That(writeThrottle.Properties["Namespace"].Value<string>(), Is.EqualTo(AwsNamespace.DynamoDb));
            Assert.That(writeThrottle.Dimension("TableName"), Is.EqualTo("first-table"));
        }


        [Test]
        public async Task CanOverrideThresholdPercentage()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                {
                    Resources =
                        new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Pattern = "first-table",
                                Values = new Dictionary<string, AlarmValues>()
                                {
                                    {"GsiConsumedReadCapacityUnitsHigh", 20},
                                    {"ConsumedReadCapacityUnitsHigh", 10}
                                }
                            }
                        }
                }
            });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "production-first-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 100,
                        WriteCapacityUnits = 200
                    },
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>()
                    {
                        new GlobalSecondaryIndexDescription()
                        {
                             IndexName = "first-gsi",
                             ProvisionedThroughput = new ProvisionedThroughputDescription()
                             {
                                 ReadCapacityUnits = 400,
                                 WriteCapacityUnits = 500
                             }
                        }
                    }
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByGsi = cloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("GlobalSecondaryIndexName");
            var gsiAlarms = alarmsByGsi["first-gsi"];

            var consumedReadGsi = gsiAlarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-first-gsi-GsiConsumedReadCapacityUnitsHigh"));
            Assert.That(consumedReadGsi.Properties["Threshold"].Value<int>(),
                Is.EqualTo(400 * OneMinuteInSeconds * 0.2m));

            var alarmsByTable = cloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("TableName");
            var tableAlarms = alarmsByTable["production-first-table"];

            var consumedReadForTable = tableAlarms.SingleOrDefault(
                a => a.Properties["AlarmName"].ToString()
                    .Contains("first-table-ConsumedReadCapacityUnitsHigh"));
            Assert.That(consumedReadForTable.Properties["Threshold"].Value<int>(),
                Is.EqualTo(100 * OneMinuteInSeconds * 0.1m));
        }

        [Test]
        public async Task GsiLogicalResourceNameContainsTable()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                {
                    Resources =
                        new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                 Pattern = "first-table",
                                 Values = new Dictionary<string, AlarmValues>()
                                 {
                                    {"GsiConsumedReadCapacityUnitsHigh", 20},
                                    {"ConsumedReadCapacityUnitsHigh", 10}
                                 }
                            }
                        }
                }
            });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "production-first-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 100,
                        WriteCapacityUnits = 200
                    },
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>()
                    {
                        new GlobalSecondaryIndexDescription()
                        {
                             IndexName = "first-gsi",
                             ProvisionedThroughput = new ProvisionedThroughputDescription()
                             {
                                 ReadCapacityUnits = 400,
                                 WriteCapacityUnits = 500
                             }
                        }
                    }
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var resources = cloudFormation
                .Stack("Watchman-test")
                .Resources;

            // TODO: logical name should include table and GSI name
            Assert.That(resources, Contains.Key("productionfirsttableGsiConsumedReadCapacityUnitsHigh"));
        }
    }
}
