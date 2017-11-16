using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingThrottlingReadAlarmsWithAThreshold
    {
        [Test]
        public async Task ReadAlarmsAreCreatedForEachTable()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "test-a-table-ReadThrottleEvents-TestGroup",
                tableName: "test-a-table",
                metricName: "ReadThrottleEvents",
                threshold: 12,
                period: 60);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new[] { "test-a-table" });

            mockery.GivenATable("test-a-table", 1300, 600);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var throttledWithThreshold = new DynamoDb
            {
                MonitorThrottling = true,
                ThrottlingThreshold = 12,
                Tables = new List<Table>
                {
                    new Table { Pattern = ".*" }
                }
            };

            return AlertingGroupData.WrapDynamo(throttledWithThreshold);
        }
    }
}
