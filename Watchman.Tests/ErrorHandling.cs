using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AutoScaling.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.AutoScaling;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Watchman.Tests.Fakes;

namespace Watchman.Tests
{
    [TestFixture]
    public class ErrorHandling
    {
        [Test]
        public async Task ContinuesWhenAlarmGenerationFailsForOneAlertingGroup()
        {
            // arrange

            var stack = new FakeStackDeployer();

            var autoScalingClient = FakeAwsClients.CreateAutoScalingClientForGroups(new[]
            {
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = "group-1",
                    DesiredCapacity = 40
                },
                new AutoScalingGroup()
                {
                    AutoScalingGroupName = "group-2",
                    DesiredCapacity = 10
                }
            });

            var source = new AutoScalingGroupSource(autoScalingClient);

            var creator = new CloudFormationAlarmCreator(stack, new ConsoleAlarmLogger(true));

            var config1 = new AlertingGroup()
            {
                Name = "group-1",
                AlarmNameSuffix = "suffix-1",
                Targets = new List<AlertTarget>()
                {
                    new AlertEmail("test@example.com")
                },
                Services = new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "group-1",
                                Options = new AutoScalingResourceConfig()
                                {
                                    // this will trigger the failure
                                    InstanceCountIncreaseDelayMinutes = 5
                                }
                            }
                        }
                    }
                }
            };
            
            var config2 = new AlertingGroup()
            {
                Name = "group-2",
                AlarmNameSuffix = "suffix-2",
                Targets = new List<AlertTarget>()
                {
                    new AlertEmail("test@example.com")
                },
                Services = new AlertingGroupServices()
                {
                    AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>()
                        {
                            new ResourceThresholds<AutoScalingResourceConfig>()
                            {
                                Name = "group-2"
                            }
                        }
                    }
                }
            };

            var cloudWatch = new Mock<IAmazonCloudWatch>();
            cloudWatch
                .Setup(c => c.GetMetricStatisticsAsync(It.IsAny<GetMetricStatisticsRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("something bad"));


            var provider = new AutoScalingGroupAlarmDataProvider(cloudWatch.Object, new CurrentTimeProvider());

            var sut = IoCHelper.CreateSystemUnderTest(
                source,
                provider,
                provider,
                WatchmanServiceConfigurationMapper.MapAutoScaling,
                creator,
                ConfigHelper.ConfigLoaderFor(config1, config2)
                );

            Exception caught = null;

            // act
            try
            {
                await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            // assert
            Assert.That(stack.StackWasDeployed("Watchman-group-1"), Is.EqualTo(false));
            Assert.That(stack.StackWasDeployed("Watchman-group-2"), Is.EqualTo(true));
            Assert.That(caught, Is.Not.Null);
        }
    }
}
