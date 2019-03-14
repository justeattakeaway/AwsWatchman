using System.Collections.Generic;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingMultipleIndexAlarmsAndOneFails
    {
        [Test]
        public void TheFirstAlarmIsPut()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            TestRun(mockery);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "happy-table-1-happy-index-1-ConsumedReadCapacityUnits-TestGroup",
                tableName: "happy-table-1",
                indexName: "happy-index-1",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 312000,
                period: 300);
        }

        [Test]
        public void NoWriteAlarmIsPut()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            TestRun(mockery);

            CloudwatchVerify.AlarmWasNotPutOnMetric(mockery.Cloudwatch, "ConsumedWriteCapacityUnits");
        }

        [Test]
        public void FurtherAlarmsAreNotPut()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            TestRun(mockery);

            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch, "failure-table-1");
            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch, "happy-table-2");
        }

        private void TestRun(DynamoAlarmGeneratorMockery mockery)
        {
            ConfigureTables(mockery);
            var generator = mockery.AlarmGenerator;
            Assert.That(async () => await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms),
                Throws.Exception);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenATableWithIndex("happy-table-1", "happy-index-1", 1300, 600);
            mockery.GivenATableWithIndex("happy-table-2", "happy-index-2", 50, 15);
            mockery.GivenATableDoesNotExist("failure-table-1");

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var listTables = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Name = "happy-table-1"},
                    new Table { Name = "failure-table-1"},
                    new Table { Name = "happy-table-2"}
                }
            };

            return AlertingGroupData.WrapDynamo(listTables);
        }
    }
}
