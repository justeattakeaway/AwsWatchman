using System.Collections.Generic;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingThrottlingWriteAlarmsForTables
    {
        [Test]
        public async void WriteAlarmsAreCreatedForEachTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-WriteThrottleEvents-TestGroup",
                tableName: "test-a-table",
                metricName: "WriteThrottleEvents",
                threshold: 2,
                period: 60);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 135000,
                period: 300);
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
                    new Table { Pattern = ".*", Threshold = 0.75 }
                }
            };

            return AlertingGroupData.WrapDynamo(allTablesReadOnly);
        }
    }
}
