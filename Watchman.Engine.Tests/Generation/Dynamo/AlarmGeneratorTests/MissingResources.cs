using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class MissingResources
    {
        [Test]
        public async Task NamedTableThatDoesNotExistIsIgnored()
        {

            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            var config = new WatchmanConfiguration()
            {
                AlertingGroups = new List<AlertingGroup>()
                {
                    new AlertingGroup()
                    {
                        Name = "TestGroup",
                        AlarmNameSuffix = "TestGroup",
                        DynamoDb = new DynamoDb()
                        {
                            Tables = new List<Table>
                            {
                                new Table { Name = "table-that-exists" },
                                new Table { Name = "missing-table" }
                            }
                        }
                    }
                }
            };

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                alarmName: "table-that-exists-ConsumedWriteCapacityUnits-TestGroup",
                tableName: "table-that-exists",
                metricName: "ConsumedWriteCapacityUnits",
                threshold: 144000,
                period: 300);
        }

        private static void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.GivenATable("table-that-exists", 1300, 600);
            mockery.ValidSnsTopic();
        }

        [Test]
        public async Task DoesNotThrowIfTableIsReturnedInListingButCannotBeDescribed()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            mockery.GivenAListOfTables(new[] { "banana" , "apple"});
            mockery.GivenATable("apple", 1300, 600);
            mockery.ValidSnsTopic();

            var config = new WatchmanConfiguration()
            {
                AlertingGroups = new List<AlertingGroup>()
                {
                    new AlertingGroup()
                    {
                        Name = "TestGroup",
                        AlarmNameSuffix = "TestGroup",
                        DynamoDb = new DynamoDb()
                        {
                            Tables = new List<Table>
                            {
                                new Table { Pattern = "^.*$" }
                            }
                        }
                    }
                }
            };

            await generator.GenerateAlarmsFor(config, RunMode.GenerateAlarms);
        }
    }
}
