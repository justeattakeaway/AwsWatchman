using System.Collections.Generic;
using System.Linq;
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
        private IAmazonDynamoDB CreateDynamoClientForTables(IEnumerable<TableDescription> tables)
        {
            tables = tables.ToList();

            var fakeDynamo = new Mock<IAmazonDynamoDB>();

            fakeDynamo
                .Setup(x => x.ListTablesAsync((string)null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListTablesResponse()
                {
                    TableNames = tables.Select(t => t.TableName).ToList()
                });

            foreach (var table in tables)
            {
                fakeDynamo
                    .Setup(x => x.DescribeTableAsync(table.TableName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DescribeTableResponse()
                    {
                        Table = table
                    });

            }

            return fakeDynamo.Object;
        }

        private IAmazonLambda CreateLambdaClientForFunctions(IEnumerable<FunctionConfiguration> functions)
        {
            var fakeLambda = new Mock<IAmazonLambda>();
            fakeLambda
                .Setup(l => l.ListFunctionsAsync(It.IsAny<ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListFunctionsResponse()
                {
                    Functions = functions.ToList()
                });

            return fakeLambda.Object;
        }

        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange

            var fakeStackDeployer = new FakeStackDeployer();

            var dynamoClient = CreateDynamoClientForTables(new[]
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

            var lambdaClient = CreateLambdaClientForFunctions(new[]
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
                new Dictionary<string, AwsServiceAlarms>()
                {
                    {
                        "DynamoDb", new AwsServiceAlarms()
                        {
                            Resources = new List<ResourceThresholds>()
                            {
                                new ResourceThresholds()
                                {
                                    Name = "first-dynamo-table"
                                }
                            }
                        }
                    },
                    {
                        "Lambda", new AwsServiceAlarms()
                        {
                            Resources = new List<ResourceThresholds>()
                            {
                                new ResourceThresholds()
                                {
                                    Name = "first-lambda-function"
                                }
                            }
                        }
                    }
                }
            );

            var sutBuilder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);

            sutBuilder.AddService(
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
