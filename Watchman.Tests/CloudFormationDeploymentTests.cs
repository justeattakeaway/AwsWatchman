using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests
{
    public class CloudFormationDeploymentTests
    {
        [Test]
        public async Task DoesNotDeployNewEmptyStack()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "non-existent-resource"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new AutoScalingGroup[0]);


            var now = DateTime.Parse("2018-01-26");

            ioc.GetMock<ICurrentTimeProvider>()
                .UtcNow
                .Returns(now);


            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var stack = cloudformation
                .Stack("Watchman-test");

            Assert.That(stack, Is.Null);
        }

        [Test]
        public async Task DoesDeployEmptyStackIfAlreadyPresentButResourcesNoLongerExist()
        {
            // first create a stack which has a resource

            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "group-1"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();

            var firstTestContext = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            firstTestContext.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new[]
            {
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = "group-1",
                    DesiredCapacity = 40
                }
            });

            await firstTestContext.Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // check it got deployed

            var stack = cloudformation
                .Stack("Watchman-test");

            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.Resources
                .Values
                .Where(r => r.Type == "AWS::CloudWatch::Alarm")
                .Count, Is.GreaterThan(0));

            var secondTestContext = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            // no matching resource so the next run results in an empty stack

            secondTestContext.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new AutoScalingGroup[0]);

            await secondTestContext
                .Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            stack = cloudformation
                .Stack("Watchman-test");

            //check we deployed an empty stack and deleted the redundant resources
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.Resources.Any(x => x.Value.Type == "AWS::CloudWatch::Alarm"), Is.False);
        }

        [Test]
        public async Task DoesDeployEmptyStackIfAlreadyPresentButConfigNowEmpty()
        {
            // first create a stack which has a resource

            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "group-1"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();

            var firstTestContext = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            firstTestContext.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new[]
            {
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = "group-1",
                    DesiredCapacity = 40
                }
            });

            await firstTestContext.Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // check it got deployed

            var stack = cloudformation
                .Stack("Watchman-test");

            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.Resources
                .Values
                .Where(r => r.Type == "AWS::CloudWatch::Alarm")
                .Count, Is.GreaterThan(0));

            // deploy empty config over the top

            var emptyConfig = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices());

            var secondTestContext = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(emptyConfig);


            await secondTestContext
                .Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            stack = cloudformation
                .Stack("Watchman-test");

            //check we deployed an empty stack and deleted the redundant resources
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.Resources.Any(x => x.Value.Type == "AWS::CloudWatch::Alarm"), Is.False);
        }

        [Test]
        public async Task TestNullRefIssueWhenMixingOldAndNewConfig()
        {
            var config = new WatchmanConfiguration()
            {
                AlertingGroups = new List<AlertingGroup>()
                {
                    new AlertingGroup()
                    {
                        Name = "ag1",
                        AlarmNameSuffix = "ag1suffix",
                        Services = null,
                        DynamoDb = new DynamoDb()
                        {
                            Tables = new List<Table>()
                            {
                                new Table()
                                {
                                    Name = "banana"
                                }
                            }
                        }
                    },
                    new AlertingGroup()
                    {
                        Name = "ag2",
                        AlarmNameSuffix = "ag2suffix",

                        Services = new AlertingGroupServices()
                        {
                            AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                            {
                                Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                                {
                                    new ResourceThresholds<AutoScalingResourceConfig>()
                                    {
                                        Name = "asg"
                                    }
                                }
                            }
                        }
                    }

                }
            };

            var cloudformation = new FakeCloudFormation();

            var context = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            context.GetMock<IAmazonAutoScaling>()
                .HasAutoScalingGroups(new AutoScalingGroup[0]);

            await context.Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // pass as did not throw
        }

        [Test]
        public async Task DoesDeployMultipleStacksIfSelected()
        {
            // first create a stack which has a resource

            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "group-1"
                            }
                        }
                    }
                },
                2);

            var cloudformation = new FakeCloudFormation();

            var firstTestContext = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            firstTestContext.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new[]
            {
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = "group-1",
                    DesiredCapacity = 40
                }
            });

            await firstTestContext.Get<AlarmLoaderAndGenerator>()
                .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // check it got deployed

            var stack = cloudformation
                .Stack("Watchman-test");

            Assert.That(stack, Is.Not.Null);
            var stack1Alarms = stack.Resources
                .Values
                .Where(r => r.Type == "AWS::CloudWatch::Alarm")
                .ToArray();

            Assert.That(stack1Alarms, Is.Not.Empty);

            // first stack alarms shouldn't be prefixed
            Assert.That(stack1Alarms.Any(a => a.Properties["AlarmName"].ToObject<string>().EndsWith("-0")), Is.False);

            var stack2 = cloudformation
                .Stack("Watchman-test-1");

            Assert.That(stack2, Is.Not.Null);

            var stack2Alarms = stack2.Resources
                .Values
                .Where(r => r.Type == "AWS::CloudWatch::Alarm")
                .ToArray();
            Assert.That(stack2Alarms, Is.Not.Empty);
            Assert.That(stack2Alarms.All(a => a.Properties["AlarmName"].ToObject<string>().EndsWith("-1")), Is.True);
        }

        [Test]
        public async Task AlarmsAreDistributedEvenlyAcrossStacks()
        {
            const int numStacks = 10;
            const int numAsgs = 100;

            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Pattern = ".*"
                            }
                        }
                    }
                },
                numberOfCloudFormationStacks: numStacks);

            var cloudFormation = new FakeCloudFormation();

            var context = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            var lotsOfAsgs = Enumerable.Range(0, numAsgs).Select(r =>

                    new AutoScalingGroup()
                    {
                        AutoScalingGroupName = $"group-{r}",
                        DesiredCapacity = 40
                    }
                )
                .ToArray();

            context.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(lotsOfAsgs);

            try
            {
                await context.Get<AlarmLoaderAndGenerator>()
                    .LoadAndGenerateAlarms(RunMode.GenerateAlarms);
            }
            catch
            {
                // ignore
            }

            var stacks = cloudFormation.Stacks();

            Assert.That(stacks.Count, Is.EqualTo(numStacks));

            var resourceCountsByStack = stacks.Select(s => (s.name, s.template.Resources.Count)).ToArray();

            var totalResources = stacks.Sum(s => s.template.Resources.Count);

            var alarmCount = stacks
                .Sum(s => s.template.Resources.Count(r => r.Value.Type == "AWS::CloudWatch::Alarm"));

            Assert.That(alarmCount, Is.EqualTo(Defaults.AutoScaling.Count * numAsgs));

            var approxExpectedPerStack = (float) totalResources / numStacks;

            foreach (var (_, count) in resourceCountsByStack)
            {
                Assert.That(count, Is.Not.GreaterThan(approxExpectedPerStack * 1.2));
                Assert.That(count, Is.Not.LessThan(approxExpectedPerStack * 0.8));
            }
        }

        [Test]
        public async Task StacksAreNotListedMultipleTimes()
        {
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Pattern = ".*"
                            }
                        }
                    }
                },
                numberOfCloudFormationStacks: 5);

            var cloudFormation = new FakeCloudFormation();

            var context = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            context.GetMock<IAmazonAutoScaling>().HasAutoScalingGroups(new[]
            {
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = $"group-asg",
                    DesiredCapacity = 40
                }
            });

            await context.Get<AlarmLoaderAndGenerator>()
                    .LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            var stacks = cloudFormation.Stacks();

            Assert.That(stacks.Count, Is.GreaterThan(1));
            Assert.That(cloudFormation.CallsToListStacks, Is.EqualTo(1));
        }
    }
}
