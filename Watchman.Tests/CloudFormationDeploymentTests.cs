using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
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
                .Setup(f => f.UtcNow)
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
        public async Task DoesDeployEmptyStackIfAlreadyPresent()
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
            Assert.That(stack.Resources.Count, Is.GreaterThan(0));

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
            Assert.That(stack.Resources.Count, Is.Zero);
        }
    }
}
