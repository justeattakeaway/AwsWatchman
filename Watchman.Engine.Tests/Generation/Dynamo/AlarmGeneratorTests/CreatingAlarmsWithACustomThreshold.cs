using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingAlarmsWithACustomThreshold
    {
        private const int ReadCapacity = 1000;
        private const int WriteCapacity = 2000;

        [Test]
        public async Task CorrectAlarmsAreCreatedForEachTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(ConfigWithThresholds(), RunMode.GenerateAlarms);

            var expectedTable1ReadThreshold = (int)AlarmThresholds.Calulate(ReadCapacity, 0.4);
            var expectedTable1WriteThreshold = (int)AlarmThresholds.Calulate(WriteCapacity, 0.4);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test1-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test1",
                metricName: "ConsumedReadCapacityUnits",
                threshold: expectedTable1ReadThreshold,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test1-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test1",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: expectedTable1WriteThreshold,
                period: 300);

            var expectedTable2ReadThreshold = (int)AlarmThresholds.Calulate(ReadCapacity, 0.65);
            var expectedTable2WriteThreshold = (int)AlarmThresholds.Calulate(WriteCapacity, 0.65);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test2-ConsumedReadCapacityUnits-TestGroup",
                tableName: "test2",
                metricName: "ConsumedReadCapacityUnits",
                threshold: expectedTable2ReadThreshold,
                period: 300);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test2-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "test2",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: expectedTable2WriteThreshold,
                period: 300);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[] { "test1", "test2" });

            mockery.GivenATable("test1", ReadCapacity, WriteCapacity);
            mockery.GivenATable("test2", ReadCapacity, WriteCapacity);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration ConfigWithThresholds()
        {
            var alerts = new DynamoDb
            {
                // custom threshold for all tables in the alerting group, should apply to table1.
                // Table2 overrides it again
                Threshold = 0.40,
                Tables = new List<Table>
                    {
                        new Table { Name = "test1" },
                        new Table { Name = "test2", Threshold = 0.65 }
                    }
            };

            return AlertingGroupData.WrapDynamo(alerts);
        }
    }
}
