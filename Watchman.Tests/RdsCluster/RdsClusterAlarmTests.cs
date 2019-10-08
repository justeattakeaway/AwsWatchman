using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace Watchman.Tests.RdsCluster
{
    public class RdsClusterAlarmTests
    {
        [Test]
        public async Task AlarmCreatedWithCorrectDefaults()
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
                            Pattern = "rdscluster-test"
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
                        && alarm.Properties["AlarmName"].Value<string>().Contains("CPUUtilizationHigh")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 60
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 5
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Rds
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.Missing
                )
            );
        }

        [Test]
        public async Task AlarmCreatedWithCorrectOverrides()
        {
            // arrange
            var config = ConfigHelper.CreateBasicConfiguration("test", "group-suffix", new AlertingGroupServices
            {
                Lambda = new AwsServiceAlarms<ResourceConfig>
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
                                                    evaluationPeriods : 1,
                                                    periodMinutes : 5,
                                                    value : 70
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
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 1
                        && alarm.Properties["Threshold"].Value<int>() == 70)
            );
        }
    }
}
