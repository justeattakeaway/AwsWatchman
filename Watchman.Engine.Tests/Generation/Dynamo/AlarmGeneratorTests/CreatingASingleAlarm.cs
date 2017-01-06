using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class CreatingASingleAlarm
    {
        [Test]
        public async Task AReadAlarmIsCreatedOrUpdated()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "authentication-tokens-ThisIsAnIndex-ConsumedReadCapacityUnits-TestGroup",
                tableName: "authentication-tokens",
                indexName: "ThisIsAnIndex",
                metricName: "ConsumedReadCapacityUnits",
                threshold: 312000,
                period: 300);
        }

        [Test]
        public async Task AWriteAlarmIsCreatedOrUpdated()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnIndex(mockery.Cloudwatch,
                alarmName: "authentication-tokens-ThisIsAnIndex-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "authentication-tokens",
                indexName: "ThisIsAnIndex",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 144000,
                period: 300);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenAListOfTables(new string[] { "authentication-tokens" });
            mockery.GivenATableWithIndex("authentication-tokens", "ThisIsAnIndex", 1300, 600);

            mockery.ValidSnsTopic();
        }

        private static WatchmanConfiguration Config()
        {
            var ag = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table { Name = "authentication-tokens" }
                }
            };

            return AlertingGroupData.WrapDynamo(ag);
        }
    }
}
