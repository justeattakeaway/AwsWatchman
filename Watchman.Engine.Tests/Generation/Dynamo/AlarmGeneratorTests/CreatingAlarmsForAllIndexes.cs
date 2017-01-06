using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    public class CreatingAlarmsForAllIndexes
    {
        [Test]
        public async Task AlarmsAreCreatedForEachTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumerIdIndex-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test-a-table",
                indexName: "ConsumerIdIndex",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 312000,
                period:300);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "test-a-table-ConsumerIdIndex-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test-a-table",
                indexName: "ConsumerIdIndex",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 144000,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "products-table-ThisIsAnIndex-ConsumedReadCapacityUnits-TestGroup",
                tableName: "products-table",
                indexName: "ThisIsAnIndex",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 108480,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "products-table-ThisIsAnIndex-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "products-table",
                indexName: "ThisIsAnIndex",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 132000,
                period: 300);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(
                new[] { "test-a-table", "customer-table", "test-supplier-table", "products-table" });

            mockery.GivenATableWithIndex("test-a-table", "ConsumerIdIndex", 1300, 600);
            mockery.GivenATable("customer-table", 2800, 1130);
            mockery.GivenATable("test-supplier-table", 400, 100);
            mockery.GivenATableWithIndex("products-table", "ThisIsAnIndex", 452, 550);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var ag = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                }
            };

            return AlertingGroupData.WrapDynamo(ag);
        }
    }
}
