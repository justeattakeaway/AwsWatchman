using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;
using Watchman.Engine.Tests.Generation.Sqs;

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
    }
}
