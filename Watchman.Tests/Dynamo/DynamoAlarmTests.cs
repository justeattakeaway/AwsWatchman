using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Watchman.Tests.Fakes;

namespace Watchman.Tests.Dynamo
{
    public class DynamoAlarmTests
    {
        private const int OneMinuteInSeconds = 60;

        [Test]
        public async Task IgnoresNamedEntitiesThatDoNotExist()
        {
            // arrange

            var stack = new Mock<ICloudformationStackDeployer>();

            var dynamoClient = FakeAwsClients.CreateDynamoClientForTables(new[]
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

            var source = new TableDescriptionSource(dynamoClient);

            var creator = new CloudFormationAlarmCreator(stack.Object, new ConsoleAlarmLogger(true));

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

            var builder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);
            builder.AddDynamoDbService(source,
                new DynamoDbDataProvider(),
                new DynamoDbDataProvider(),
                WatchmanServiceConfigurationMapper.MapDynamoDb
            );

            var sut = builder.Build();



            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);
            
            // assert

            stack
                .Verify(x => x.DeployStack(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                    ), Times.Never);
        }

        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange

            var stack = new FakeStackDeployer();

            var dynamoClient = FakeAwsClients.CreateDynamoClientForTables(new[]
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

            var source = new TableDescriptionSource(dynamoClient);
            var creator = new CloudFormationAlarmCreator(stack, new ConsoleAlarmLogger(true));

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

            var builder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);
            builder.AddDynamoDbService(source,
                new DynamoDbDataProvider(),
                new DynamoDbDataProvider(),
                WatchmanServiceConfigurationMapper.MapDynamoDb
                );

            var sut = builder.Build();
            
            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            const decimal defaultCapacityThreshold = 0.8m;
            const decimal capacityMultiplier = defaultCapacityThreshold * OneMinuteInSeconds;
            const int defaultThrottleThreshold = 2;

            var alarmsByTable = stack
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

            Assert.That(alarms.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "ThrottledRequests"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("ThrottledRequestsHigh")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == defaultThrottleThreshold
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Sum"
                    && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.DynamoDb
                )
            );
        }

        [Test]
        public async Task AlarmsCanBeAddedToGlobalSecondaryIndexes()
        {
            // arrange

            var stack = new FakeStackDeployer();

            var dynamoClient = FakeAwsClients.CreateDynamoClientForTables(new[]
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

            var source = new TableDescriptionSource(dynamoClient);
            var creator = new CloudFormationAlarmCreator(stack, new ConsoleAlarmLogger(true));

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

            var builder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);
            builder.AddDynamoDbService(source,
                new DynamoDbDataProvider(),
                new DynamoDbDataProvider(),
                WatchmanServiceConfigurationMapper.MapDynamoDb
            );

            var sut = builder.Build();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert
            const decimal defaultCapacityThreshold = 0.8m;
            const decimal capacityMultiplier = defaultCapacityThreshold * OneMinuteInSeconds;
            const int defaultThrottleThreshold = 2;

            // basic check we still have table alarms
            Assert.That(stack.Stack("Watchman-test").AlarmsByDimension("TableName").Any(), Is.True);

            var alarmsByGsi = stack
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
        }
    }
}
