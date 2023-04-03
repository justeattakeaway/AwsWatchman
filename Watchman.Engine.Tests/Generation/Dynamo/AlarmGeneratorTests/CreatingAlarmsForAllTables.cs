using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingAlarmsForAllTables
    {
        [Test]
        public async Task AlarmsAreCreatedForEachTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            ConfigureTables(mockery);

            var generator = mockery.AlarmGenerator;

            await generator.GenerateAlarmsFor(CatchAllConfig(), RunMode.GenerateAlarms);

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

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-supplier-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-supplier-table",
                metricName: "ConsumedReadCapacityUnits");

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-supplier-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-supplier-table",
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
        public async Task AlarmsAreCreatedForEachTableAtCorrectLevel()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            ConfigureTables(mockery);

            var generator = mockery.AlarmGenerator;

            await generator.GenerateAlarmsFor(CatchAllConfig(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 312000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-a-table",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 144000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 672000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "customer-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "customer-table",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 271200,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-supplier-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-supplier-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 96000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-supplier-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-supplier-table",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 24000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "products-table-ConsumedReadCapacityUnits-TestGroup",
                tableName: "products-table",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 576000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "products-table-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "products-table",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 19200,
                period: 300);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[]
            {
                "test-a-table", "customer-table", "test-supplier-table", "products-table"
            });

            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("customer-table", 2800, 1130);
            mockery.GivenATable("test-supplier-table", 400, 100);
            mockery.GivenATable("products-table", 2400, 80);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration CatchAllConfig()
        {
            var catchAll = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                }
            };

            return AlertingGroupData.WrapDynamo(catchAll);
        }
    }
}
