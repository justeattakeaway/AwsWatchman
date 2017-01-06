using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingAlarmsForAllTablesWithExclusions
    {
        [Test]
        public async Task AlarmsAreCreatedForEachIncludedTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithAnExclusion(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedWriteCapacityUnits");
        }

        [Test]
        public async Task AlarmsAreNotCreatedForExcludedTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithAnExclusion(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch, "products-table");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[]
            {
                "test-a-table", "customer-table", "products-table"
            });

            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("customer-table", 2800, 1130);
            mockery.GivenATable("products-table", 2400, 80);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration ConfigWithAnExclusion()
        {
            var dynamo = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                },
                ExcludeTablesPrefixedWith = new List<string> {"products-table"}
            };

            return AlertingGroupData.WrapDynamo(dynamo);
        }
    }
}
