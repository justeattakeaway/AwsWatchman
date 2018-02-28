using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
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
using Watchman.Configuration.Load;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
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

            ioc.GetMock<IConfigLoader>().HasConfig(config);

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
