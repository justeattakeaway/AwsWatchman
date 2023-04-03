using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.CloudFront
{
    [TestFixture]
    public class CloudFrontAlarmTests
    {
        [Test]
        public async Task IgnoresNamedEntitiesThatDoNotExist()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices
                {
                    CloudFront = new AwsServiceAlarms<ResourceConfig>
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>
                        {
                            new ResourceThresholds<ResourceConfig>
                            {
                                Name = "non-existent-distribution"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudFront>().HasCloudFrontDistributions(new []
            {
                new DistributionSummary
                {
                    Id = "distribution-1"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();
            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            Assert.That(cloudformation.StacksDeployed, Is.Zero);
        }

        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                CloudFront = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig>
                        {
                            Name = "distribution-1",
                            Values = new Dictionary<string, AlarmValues>
                            {
                                {"4xxErrorRate", new AlarmValues(10, TimeSpan.FromMinutes(5).Minutes)}
                            }
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonCloudFront>().HasCloudFrontDistributions(new[]
            {
                new DistributionSummary
                {
                    Id = "distribution-1"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();
            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            var alarmsByDistributionId = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("DistributionId");

            Assert.That(alarmsByDistributionId.ContainsKey("distribution-1"), Is.True);

            var alarms = alarmsByDistributionId["distribution-1"];

            Assert.That(alarms.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "4xxErrorRate"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("4xxErrorRate")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    ));
        }
    }
}
