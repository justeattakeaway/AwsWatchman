using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingReadAlarmsForAllTables
    {
        [Test]
        public async Task ReadAlarmsAreCreatedForEachTable()
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
                alarmName: "customer-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 630000,
                period:300);
        }

        [Test]
        public async Task NoWriteAlarmsAreCreated()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnMetric(mockery.Cloudwatch, "ConsumedWriteCapacityUnits");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[] { "test-a-table", "customer-table" });

            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("customer-table", 2800, 1130);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var group = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*", Threshold = 0.75, MonitorWrites = false }
                }
            };

            return AlertingGroupData.WrapDynamo(group);
        }
    }
}
