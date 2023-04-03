using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class DisablingCapacityAlarms
    {
        [Test]
        public async Task SkipsTableCapacityAlarmsWhenDisabledForAlertingGroup()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            var config = AlertingGroupData.WrapDynamo(new DynamoDb
            {
                MonitorThrottling = true,
                MonitorCapacity = false,
                Tables = new List<Table>
                {
                    new Table {Name = "test1"}
                }
            });

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test1",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test1",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch, "test1", "ReadThrottleEvents");
        }

        [Test]
        public async Task SkipsTableCapacityAlarmsWhenDisabledForTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            var config = AlertingGroupData.WrapDynamo(new DynamoDb
            {
                MonitorThrottling = true,
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "test1",
                        MonitorCapacity = false
                    }
                }
            });

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test1",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test1",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch, "test1", "ReadThrottleEvents");
        }

        [Test]
        public async Task TableLevelSettingCanOverrideGroupLevel()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            var config = AlertingGroupData.WrapDynamo(new DynamoDb
            {
                MonitorThrottling = true,
                MonitorCapacity = false,
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "test1",
                        MonitorCapacity = true
                    }
                }
            });

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch, "test1", "ConsumedReadCapacityUnits");
            
            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch, "test1", "ConsumedWriteCapacityUnits");
        }

        [Test]
        public async Task SkipsIndexCapacityAlarmsWhenDisabledForAlertingGroup()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            var config = AlertingGroupData.WrapDynamo(new DynamoDb
            {
                MonitorThrottling = true,
                MonitorCapacity = false,
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "test1"
                    }
                }
            });

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnIndex(mockery.Cloudwatch,
                tableName: "test1",
                indexName: "test1-index",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasNotPutOnIndex(mockery.Cloudwatch,
                tableName: "test1",
                indexName: "test1-index",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch, "test1","test1-index","ReadThrottleEvents");
        }

        [Test]
        public async Task SkipsIndexCapacityAlarmsWhenDisabledForTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            var config = AlertingGroupData.WrapDynamo(new DynamoDb
            {
                MonitorThrottling = true,
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "test1",
                        MonitorCapacity = false
                    }
                }
            });

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnIndex(mockery.Cloudwatch,
                tableName: "test1",
                indexName: "test1-index",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasNotPutOnIndex(mockery.Cloudwatch,
                tableName: "test1",
                indexName: "test1-index",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch, "test1","test1-index","ReadThrottleEvents");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[] {"test1"});
            mockery.GivenATableWithIndex("test1", "test1-index", 10, 10);
            mockery.ValidSnsTopic();
        }
    }
}
