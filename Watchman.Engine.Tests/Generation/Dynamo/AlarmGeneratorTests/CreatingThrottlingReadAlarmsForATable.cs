using System.Collections.Generic;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingThrottlingReadAlarmsForATable
    {
        [Test]
        public async void ReadAlarmsAreCreatedForEachTable()
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
                threshold: 2,
                period: 60);

            CloudwatchVerify.AlarmWasNotPutOnMetric(mockery.Cloudwatch,
                "ConsumedWriteCapacityUnits");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.ValidSnsTopic();
            mockery.GivenAListOfTables(new[] { "test-a-table" });
            mockery.GivenATable("test-a-table", 1300, 600);
        }

        private static WatchmanConfiguration Config()
        {
            var allTablesReadOnly = new DynamoDb
            {
                MonitorThrottling = true,
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*", Threshold = 0.75, MonitorWrites = false }
                }
            };

            return AlertingGroupData.WrapDynamo(allTablesReadOnly);
        }
    }
}
