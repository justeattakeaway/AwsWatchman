using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingAlarmsForAllIndexesWithExclusions
    {
        [Test]
        public async Task AlarmsAreCreatedForEachIncludedTable()
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

            CloudwatchVerify.AlarmWasNotPutonIndex(mockery.Cloudwatch,
                tableName: "products-table",
                indexName: "ThisIsAnIndex");
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(
                new[] { "test-a-table", "customer-table", "test-supplier-table", "products-table" });

            mockery.GivenATableWithIndex(tableName: "test-a-table", indexName: "ConsumerIdIndex", indexRead: 1300, indexWrite: 600);

            mockery.GivenATable("customer-table", 2800, 1130);
            mockery.GivenATable("test-supplier-table", 400, 100);
            mockery.GivenATableWithIndex(tableName: "products-table", indexName: "ThisIsAnIndex", indexRead: 452, indexWrite: 550);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var ag = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                },
                ExcludeTablesPrefixedWith = new List<string> {"products-table"}
            };

            return AlertingGroupData.WrapDynamo(ag);
        }
    }
}
