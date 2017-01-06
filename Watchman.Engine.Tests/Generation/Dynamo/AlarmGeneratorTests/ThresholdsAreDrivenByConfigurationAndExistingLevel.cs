using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    public class ThresholdsAreDrivenByConfigurationAndExistingLevel
    {
        [Test]
        public async Task TableAlarmThresholdsAreSet()
        {
            var mockery = new DynamoAlarmGeneratorMockery();
            var generator = mockery.AlarmGenerator;

            ConfigureTables(mockery);

            await generator.GenerateAlarmsFor(Config(), RunMode.GenerateAlarms);

            CloudwatchVerify.AlarmWasPutMatching(mockery.Cloudwatch,
                request =>
                request.AlarmName == "orders-ConsumedReadCapacityUnits-TestGroup"
                && request.MetricName == AwsMetrics.ConsumedReadCapacity
                && request.Threshold.Equals(4000*0.5*AwsConstants.FiveMinutesInSeconds));
        }

        private void ConfigureTables(DynamoAlarmGeneratorMockery mockery)
        {
            mockery.ValidSnsTopic();
            mockery.GivenATable("orders", 4000, 800);
        }

        private static WatchmanConfiguration Config()
        {
            var dynamoConfig = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "orders",
                        Threshold = 0.5
                    }
                }
            };

            return AlertingGroupData.WrapDynamo(dynamoConfig);
        }
    }
}
