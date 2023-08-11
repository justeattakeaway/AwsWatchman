using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests
{
    public class MetadataTests
    {
        [Test]
        public async Task AlarmsHaveMeaningfulDescription()
        {
            // arrange
            var config = new WatchmanConfiguration()
            {
                AlertingGroups = new List<AlertingGroup>()
                {
                    new AlertingGroup()
                    {
                        Name = "group-with-description",
                        AlarmNameSuffix = "group-suffix-1",
                        Description = "Group description for group 1",
                        Services = new AlertingGroupServices()
                        {
                            Elb = new AwsServiceAlarms<ResourceConfig>()
                            {
                                Resources = new List<ResourceThresholds<ResourceConfig>>()
                                {
                                    new ResourceThresholds<ResourceConfig>()
                                    {
                                        Name = "elb-1"
                                    }
                                }
                            }
                        }
                    },


                    new AlertingGroup()
                    {
                        Name = "group-without-description",
                        AlarmNameSuffix = "group-suffix-2",
                        Services = new AlertingGroupServices()
                        {
                            Elb = new AwsServiceAlarms<ResourceConfig>()
                            {
                                Resources = new List<ResourceThresholds<ResourceConfig>>()
                                {
                                    new ResourceThresholds<ResourceConfig>()
                                    {
                                        Name = "elb-2"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var fakeCloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(fakeCloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonElasticLoadBalancing>().DescribeReturnsLoadBalancers(new[]
            {
                new LoadBalancerDescription()
                {
                    LoadBalancerName = "elb-1"
                },
                new LoadBalancerDescription()
                {
                    LoadBalancerName = "elb-2"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var firstGroupAlarm = fakeCloudFormation
                .Stack("Watchman-group-with-description")
                .Alarms()
                .First();

            var description = firstGroupAlarm.Properties["AlarmDescription"].ToString();

            Assert.That(description, Contains.Substring("Alarm (new version) managed by AwsWatchman"));
            Assert.That(description, Contains.Substring("group-with-description"));
            Assert.That(description, Contains.Substring("Group description for group 1"));

            var secondGroupAlarm = fakeCloudFormation
                .Stack("Watchman-group-without-description")
                .Alarms()
                .First();

            var description2 = secondGroupAlarm.Properties["AlarmDescription"].ToString();

            Assert.That(description2, Contains.Substring("Alarm (new version) managed by AwsWatchman"));
            Assert.That(description2, Contains.Substring("group-without-description"));
        }
    }
}
