using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.AwsResources.Services.Lambda;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Watchman.Tests.Fakes;

namespace Watchman.Tests
{
    public class MultipleServiceAlarmTests
    {
        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange

            var fakeStackDeployer = new FakeStackDeployer();

            var dynamoClient = FakeAwsClients.CreateDynamoClientForTables(new[]
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

            var lambdaClient = FakeAwsClients.CreateLambdaClientForFunctions(new[]
            {
                new FunctionConfiguration()
                {
                    FunctionName = "first-lambda-function"
                }
            });

            var creator = new CloudFormationAlarmCreator(fakeStackDeployer, new ConsoleAlarmLogger(true));

            var config = ConfigHelper.CreateBasicConfiguration(
                "test",
                "group-suffix",
                new AlertingGroupServices()
                {
                    DynamoDb = new AwsServiceAlarms<ResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
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

            var sutBuilder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);

            sutBuilder.AddDynamoDbService(
                new TableDescriptionSource(dynamoClient), 
                new DynamoDbDataProvider(),
                new DynamoDbDataProvider(),
                WatchmanServiceConfigurationMapper.MapDynamoDb
                );

            sutBuilder.AddService(
                new LambdaSource(lambdaClient),
                new LambdaAlarmDataProvider(),
                new LambdaAlarmDataProvider(),
                WatchmanServiceConfigurationMapper.MapLambda
                );

            var sut = sutBuilder.Build();
            
            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByTable = fakeStackDeployer
                .Stack("Watchman-test")
                .AlarmsByDimension("TableName");

            Assert.That(alarmsByTable.ContainsKey("first-dynamo-table"), Is.True);

            var alarmsByFunction = fakeStackDeployer
                .Stack("Watchman-test")
                .AlarmsByDimension("FunctionName");

            Assert.That(alarmsByFunction.ContainsKey("first-lambda-function"), Is.True);
        }
    }
}
