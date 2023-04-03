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
    public class OptOutTests
    {
        [Test]
        public async Task CanOptOut()
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
                        Services = new AlertingGroupServices()
                        {
                            Elb = new AwsServiceAlarms<ResourceConfig>()
                            {
                                Resources = new List<ResourceThresholds<ResourceConfig>>()
                                {
                                    new ResourceThresholds<ResourceConfig>()
                                    {
                                        Name = "elb-1",
                                        Values = new Dictionary<string, AlarmValues>()
                                        {
                                            // to test we can opt out at resource level
                                            { "LatencyHigh", new AlarmValues(enabled: false) },

                                            // to test we can revert an opt-out at service level
                                            { "UnHealthyHostCountHigh", new AlarmValues(enabled: true) }
                                        }
                                    }
                                },
                                Values = new Dictionary<string, AlarmValues>()
                                {
                                    // test we can opt out at service level
                                    { "Http5xxErrorsHigh", new AlarmValues(enabled: false) },

                                    // setup for above (we can revert an opt-out at service level)
                                    { "UnHealthyHostCountHigh", new AlarmValues(enabled: false) }
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

            var alarms = fakeCloudFormation
                .Stack("Watchman-group-with-description")
                .AlarmsByDimension("LoadBalancerName");

            var alarmsForElb1 = alarms["elb-1"];

            Assert.That(alarmsForElb1.Any(
                    alarm => alarm.Properties["AlarmName"].ToString().Contains("LatencyHigh")
                ), Is.False);

            Assert.That(alarmsForElb1.Any(
                    alarm => alarm.Properties["AlarmName"].ToString().Contains("Http5xxErrorsHigh")
                ), Is.False);

            Assert.That(alarmsForElb1.Any(
                    alarm => alarm.Properties["AlarmName"].ToString().Contains("UnHealthyHostCountHigh")
                ), Is.True);
        }
    }
}
