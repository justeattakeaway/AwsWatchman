using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DAX;
using Amazon.DAX.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.Dax
{
    public class DAXAlarmTests
    {
        [Test]
        public async Task IgnoresNamedEntitiesThatDoNotExist()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix",
                new AlertingGroupServices
                {
                    Dax = new AwsServiceAlarms<ResourceConfig>
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>
                        {
                            new ResourceThresholds<ResourceConfig>
                            {
                                Name = "non-existent-cluster"
                            }
                        }
                    }
                });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDAX>().HasClusters(new[]
            {
                new Cluster
                {
                    ClusterName = "first-dax-cluster"
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            Assert.That(cloudformation.StacksDeployed, Is.Zero);
        }

        [Test]
        public async Task AlarmCreatedWithCorrectProperties()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices()
            {
                Dax = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig>
                        {
                            Name = "first-dax-cluster",
                            Values = new Dictionary<string, AlarmValues>
                            {
                                { "CPUUtilizationHigh", new AlarmValues(10) }
                            }
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonDAX>().HasClusters(new[]
            {
                new Cluster
                {
                    ClusterName = "first-dax-cluster",
                }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var alarmsByCluster = cloudformation
                .Stack("Watchman-test")
                .AlarmsByDimension("ClusterId");

            Assert.That(alarmsByCluster.ContainsKey("first-dax-cluster"), Is.True);
            var alarms = alarmsByCluster["first-dax-cluster"];

            Assert.That(alarms.Exists(
                alarm =>
                    alarm.Properties["MetricName"].Value<string>() == "CPUUtilization"
                    && alarm.Properties["AlarmName"].Value<string>().Contains("CPUUtilizationHigh")
                    && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                    && alarm.Properties["Threshold"].Value<int>() == 10
                    && alarm.Properties["Period"].Value<int>() == 60
                    && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                    && alarm.Properties["Statistic"].Value<string>() == "Average"
                    && alarm.Properties["Namespace"].Value<string>() == "AWS/DAX"
                    && alarm.Properties["TreatMissingData"].Value<string>() == "missing"
                    )
                );
        }
    }
 }
