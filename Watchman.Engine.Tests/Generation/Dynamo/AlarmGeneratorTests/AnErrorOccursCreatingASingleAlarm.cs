using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    [TestFixture]
    public class AnErrorOccursCreatingASingleAlarm
    {
        [Test]
        public void FirstTableHasAlarm()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;
            ConfigureTables(mockery);

            Assert.That(async () => await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms),
                Throws.Exception);

            CloudwatchVerify.AlarmWasPutOnTable(mockery.Cloudwatch,
                "test-a-table-ConsumedReadCapacityUnits-TestGroup",
                "test-a-table", "ConsumedReadCapacityUnits");
        }

        [Test]
        public void OtherAlarmsAreNotCreated()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;
            ConfigureTables(mockery);

            Assert.That(async () => await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms),
                Throws.Exception);
            
            CloudwatchVerify.AlarmWasNotPutOnTable(mockery.Cloudwatch,"my-orders");
        }

        private void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.ValidSnsTopic();

            mockery.GivenATable("test-a-table", 1300, 600);
            mockery.GivenATable("my-orders", 4000, 800);

            mockery.GivenATableDoesNotExist("this-table-does-not-exist");
        }
        private static WatchmanConfiguration Config()
        {
            var alertingGroup = new DynamoDb
            {
                Tables = new List<Table>
                {
                    "test-a-table",
                    "this-table-does-not-exist",
                    new Table
                    {
                        Name = "my-orders",
                        Threshold = 0.5
                    }
                }
            };

            return AlertingGroupData.WrapDynamo(alertingGroup);
        }
    }
}
