using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancing.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.Elb;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Watchman.Tests.Fakes;

namespace Watchman.Tests
{
    public class ExtendedStatistics
    {
        [Test]
        public async Task CanUseExtendedStatisticsForResource()
        {
            // arrange

            var fakeStackDeployer = new FakeStackDeployer();

            var elbClient = FakeAwsClients.CreateElbClientForLoadBalancers(new[]
            {
                new LoadBalancerDescription() 
                {
                    LoadBalancerName = "elb-1"
                }
            });

         
            var creator = new CloudFormationAlarmCreator(fakeStackDeployer, new ConsoleAlarmLogger(true));

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
                                        "LatencyHigh", new AlarmValues(null, null, "p97")
                                    }
                                }
                            }
                        }
                    }
                }
            );

            var sutBuilder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);

            sutBuilder.AddService(
                new ElbSource(elbClient),
                new ElbAlarmDataProvider(), 
                new ElbAlarmDataProvider(),
                WatchmanServiceConfigurationMapper.MapElb
                );

            var sut = sutBuilder.Build();
            
            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert
            
            var alarmsByElb = fakeStackDeployer
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

            var fakeStackDeployer = new FakeStackDeployer();

            var elbClient = FakeAwsClients.CreateElbClientForLoadBalancers(new[]
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


            var creator = new CloudFormationAlarmCreator(fakeStackDeployer, new ConsoleAlarmLogger(true));

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
                                        "LatencyHigh", new AlarmValues(null, null, "p97")
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
                                "LatencyHigh", new AlarmValues(null, null, "p99")
                            }
                        }
                    }
                }
            );

            var sutBuilder = new Builder(ConfigHelper.ConfigLoaderFor(config), creator);

            sutBuilder.AddService(
                new ElbSource(elbClient),
                new ElbAlarmDataProvider(),
                new ElbAlarmDataProvider(),
                WatchmanServiceConfigurationMapper.MapElb
                );

            var sut = sutBuilder.Build();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByElb = fakeStackDeployer
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
