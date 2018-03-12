using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.Elb;
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
    public class ExtendedStatistics
    {
        [Test]
        public async Task CanUseExtendedStatisticsForResource()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration(
                "test",
                "group-suffix",
                new AlertingGroupServices()
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
                                    {
                                        "LatencyHigh", new AlarmValues(extendedStatistic: "p97")
                                    }
                                }
                            }
                        }
                    }
                }
            );

            var fakeCloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(fakeCloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonElasticLoadBalancing>().DescribeReturnsLoadBalancers(new[]
            {
                new LoadBalancerDescription()
                {
                    LoadBalancerName = "elb-1"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();
            
            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert
            
            var alarmsByElb = fakeCloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("LoadBalancerName");

            var alarm = alarmsByElb["elb-1"].FirstOrDefault(a => a.Properties["AlarmName"].ToString().Contains("LatencyHigh"));

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.Properties.ContainsKey("Statistic"), Is.False);
            Assert.That(alarm.Properties["ExtendedStatistic"].ToString(), Is.EqualTo("p97"));
        }

        [Test]
        public async Task CanSetExtendedStatisticAtResourceOrServiceLevel()
        {
            // arrange
            var fakeCloudFormation = new FakeCloudFormation();

            var config = ConfigHelper.CreateBasicConfiguration(
                "test",
                "group-suffix",
                new AlertingGroupServices()
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
                                    {
                                        "LatencyHigh", new AlarmValues(extendedStatistic: "p97")
                                    }
                                }
                            },

                            new ResourceThresholds<ResourceConfig>()
                            {
                                Name = "elb-2"
                            }
                        },

                        // set default for whole group
                        Values = new Dictionary<string, AlarmValues>()
                        {
                            {
                                "LatencyHigh", new AlarmValues(extendedStatistic: "p99")
                            }
                        }
                    }
                }
            );

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

            var alarmsByElb = fakeCloudFormation
                .Stack("Watchman-test")
                .AlarmsByDimension("LoadBalancerName");

            // should take override
            var alarm = alarmsByElb["elb-1"].FirstOrDefault(a => a.Properties["AlarmName"].ToString().Contains("LatencyHigh"));
            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.Properties["ExtendedStatistic"].ToString(), Is.EqualTo("p97"));

            // should take default of p99
            var alarm2 = alarmsByElb["elb-2"].FirstOrDefault(a => a.Properties["AlarmName"].ToString().Contains("LatencyHigh"));
            Assert.That(alarm2, Is.Not.Null);
            Assert.That(alarm2.Properties["ExtendedStatistic"].ToString(), Is.EqualTo("p99"));
        }
    }
}
