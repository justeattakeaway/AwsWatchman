using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingAlarmsForAllTablesWithReadsOrWritesExclusions
    {
        [Test]
        public async Task BothAlarmsAreCreatedForTablesNotExcluded()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithExclusions(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedWriteCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "products-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "products-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "products-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "products-table",
                metricName: "ConsumedWriteCapacityUnits");
        }

        [Test]
        public async Task OnlyWriteAlarmIsCreatedWhenReadExcluded()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithExclusions(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test-a-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedWriteCapacityUnits");
        }

        [Test]
        public async Task OnlyReadAlarmIsCreatedWhenWriteExcluded()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithExclusions(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-supplier-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-supplier-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,
                tableName: "test-supplier-table",
                metricName: "ConsumedWriteCapacityUnits");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[] { "test-a-table", "customer-table", "test-supplier-table", "products-table" });

            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("customer-table", 2800, 1130);
            mockery.GivenATable("test-supplier-table", 400, 100);
            mockery.GivenATable("products-table", 2400, 80);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration ConfigWithExclusions()
        {
            var dynamo = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                },
                ExcludeReadsForTablesPrefixedWith = new List<string> { "test-a-table" },
                ExcludeWritesForTablesPrefixedWith = new List<string> { "test-supplier-table" }
            };

            return AlertingGroupData.WrapDynamo(dynamo);
        }
    }
}
