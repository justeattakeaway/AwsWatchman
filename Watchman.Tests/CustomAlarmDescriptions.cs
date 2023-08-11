using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests
{
    public class CustomAlarmDescriptions
    {
        [Test]
        public async Task CanSetCustomAlarmDescriptionForDifferentServices()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    DynamoDb = new AwsServiceAlarms<DynamoResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<DynamoResourceConfig>>()
                        {
                            new ResourceThresholds<DynamoResourceConfig>()
                            {
                                Pattern = ".*",
                                Description = "custom dynamo text"
                            }
                        }
                    },
                    Lambda = new AwsServiceAlarms<ResourceConfig>
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Pattern = ".*",
                                Description = "custom lambda text"
                            }
                        }
                    },
                    Sqs = new AwsServiceAlarms<SqsResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<SqsResourceConfig>>()
                        {
                            new ResourceThresholds<SqsResourceConfig>()
                            {
                                Pattern = ".*",
                                Description = "custom sqs text"
                            }
                        }
                    }
                });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDynamoDB>().HasDynamoTables(new[]
            {
                new TableDescription()
                {
                    TableName = "first-dynamo-table",
                    ProvisionedThroughput = new ProvisionedThroughputDescription()
                    {
                        ReadCapacityUnits = 10,
                        WriteCapacityUnits = 10
                    }
                }
            });

            ioc.GetMock<IAmazonCloudWatch>().HasSqsQueues(new[]
            {
                "first-sqs-queue",
                "first-sqs-queue_error"
            });

            ioc.GetMock<IAmazonLambda>().HasLambdaFunctions(new[]
            {
                new FunctionConfiguration  { FunctionName = "first-lambda" }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            Assert.That(cloudFormation.StacksDeployed, Is.EqualTo(1));

            var alarmsByNamespace = cloudFormation.Stacks()
                .Single()
                .template.AlarmsByNamespace();

            VerifyAlarmDescriptions(alarmsByNamespace[AwsNamespace.Lambda], "custom lambda text");
            VerifyAlarmDescriptions(alarmsByNamespace[AwsNamespace.DynamoDb], "custom dynamo text");
            VerifyAlarmDescriptions(alarmsByNamespace[AwsNamespace.Sqs], "custom sqs text");
        }

        private static void VerifyAlarmDescriptions(IReadOnlyList<Resource> resources, string expectedCustomText)
        {
            Assert.That(resources, Is.Not.Empty);
            Assert.That(resources.Select(x => x.Properties["AlarmDescription"].Value<string>()),
                Is.All.Contains(expectedCustomText));
            Assert.That(resources.Select(x => x.Properties["AlarmDescription"].Value<string>()),
                Is.All.Contains("managed by AwsWatchman"));
        }
    }
 }
