using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
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
    public class OverrideTests
    {
        [Test]
        public async Task AlarmCreatedWithStatisticOverride()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices
            {
                RdsCluster = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>()
                    {
                        new ResourceThresholds<ResourceConfig>()
                        {
                            Pattern = "rdscluster-test",
                            Values = new Dictionary<string, AlarmValues>
                            {
                                {
                                    "CPUUtilizationHigh", new AlarmValues
                                                  (
                                                    statistic:"Average"
                                                  )
                                }
                            }
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonRDS>().HasRdsClusters(new[]
            {
                new DBCluster { DBClusterIdentifier = "rdscluster-test" }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var stack = cloudformation
                .Stack("Watchman-test");

            var alarmsByRdscluster = stack
                .AlarmsByDimension("DBClusterIdentifier");

            Assert.That(alarmsByRdscluster.ContainsKey("rdscluster-test"), Is.True);
            var alarmsForCluster = alarmsByRdscluster["rdscluster-test"];

            Assert.That(alarmsForCluster.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "CPUUtilization"
                        && alarm.Properties["Statistic"].Value<string>() == "Average")
            );
        }
    }
}
