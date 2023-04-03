using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests
{
    public class MultipleServiceAlarmTests
    {
        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration(
                "test",
                "group-suffix",
                new AlertingGroupServices()
                {
                    DynamoDb = new AwsServiceAlarms<DynamoResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<DynamoResourceConfig>>()
                        {
                            new ResourceThresholds<DynamoResourceConfig>()
                            {
                                Name = "first-dynamo-table"
                            }
                        }
                    },
                    Lambda = new AwsServiceAlarms<ResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Name = "first-lambda-function"
                            }
                        }
                    }
                }
            );

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "first-dynamo-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 100,
                        WriteCapacityUnits = 200
                    }
                }
            });

            ioc.GetMock<IAmazonLambda>().HasLambdaFunctions(new[]
            {
                new FunctionConfiguration()
                {
                    FunctionName = "first-lambda-function"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByTable = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("TableName");

            Assert.That(alarmsByTable.ContainsKey("first-dynamo-table"), Is.True);

            var alarmsByFunction = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("FunctionName");

            Assert.That(alarmsByFunction.ContainsKey("first-lambda-function"), Is.True);
        }
    }
}
