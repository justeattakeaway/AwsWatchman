using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingThrottlingAlarmsPerTable
    {
        [Test, Ignore("This test is broken")]
        public async Task AlarmsAreCreatedForEnabledTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 292500,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ReadThrottleEvents-TestGroup",
                tableName: "test-a-table",
                metricName: "ReadThrottleEvents",
                threshold: 42,
                period: 60);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-WriteThrottleEvents-TestGroup",
                tableName: "test-a-table",
                metricName: "WriteThrottleEvents",
                threshold: 42,
                period: 60);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "other-table",
                metricName: "ReadThrottleEvents");

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "other-table",
                metricName: "WriteThrottleEvents");

            CloudwatchVerify.AlarmWasNotPutOnMetric(mockery.Cloudwatch,
                "ConsumedWriteCapacityUnits");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.ValidSnsTopic();
            mockery.GivenAListOfTables(new[] { "test-a-table", "other-table" });
            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("other-table", 1300, 600);
        }

        private static WatchmanConfiguration Config()
        {
            var allTablesReadOnly = new DynamoDb
            {
                // these should be overridden below for 1 of the 2 tables
                MonitorThrottling = false,
                ThrottlingThreshold = 123,

                Tables = new List<Table>
                {
                    new Table
                    {
                        Pattern = "test",
                        Threshold = 0.75,
                        MonitorThrottling = true,
                        ThrottlingThreshold = 42
                    }
                }
            };

            return AlertingGroupData.WrapDynamo(allTablesReadOnly);
        }
    }
}
