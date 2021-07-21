using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.Lambda
{
    public class LambdaAlarmTests
    {
        [Test]
        public async Task AlarmCreatedWithCorrectDefaults()
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
                            Pattern = "lambda-test"
                        }
                    }
                }
            });

            var cloudformation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudformation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonLambda>().HasLambdaFunctions(new[]
            {
                new FunctionConfiguration  { FunctionName = "lambda-test", Timeout = 10 }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var stack = cloudformation
                .Stack("Watchman-test");

            var alarmsByFunction = stack
                .AlarmsByDimension("FunctionName");

            Assert.That(alarmsByFunction.ContainsKey("lambda-test"), Is.True);
            var alarmsForLambda = alarmsByFunction["lambda-test"];

            Assert.That(alarmsForLambda.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "Errors"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("ErrorsHigh")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 3
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 1
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Lambda
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
                )
            );

            Assert.That(alarmsForLambda.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "Duration"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("DurationHigh")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 10 * 1000 * 50/100
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 1
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Average"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Lambda
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.Missing
                )
            );

            Assert.That(alarmsForLambda.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "Throttles"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("ThrottlesHigh")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 5
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 1
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Sum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Lambda
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
                )
            );

            Assert.That(alarmsForLambda.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "IteratorAge"
                        && alarm.Properties["AlarmName"].Value<string>().Contains("IteratorAgeHigh")
                        && alarm.Properties["AlarmName"].Value<string>().Contains("-group-suffix")
                        && alarm.Properties["Threshold"].Value<int>() == 300000
                        && alarm.Properties["Period"].Value<int>() == 60 * 5
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 1
                        && alarm.Properties["ComparisonOperator"].Value<string>() == "GreaterThanOrEqualToThreshold"
                        && alarm.Properties["Statistic"].Value<string>() == "Maximum"
                        && alarm.Properties["Namespace"].Value<string>() == AwsNamespace.Lambda
                        && alarm.Properties["TreatMissingData"].Value<string>() == TreatMissingDataConstants.NotBreaching
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
                            Pattern = "lambda-test",
                            Values = new Dictionary<string, AlarmValues>
                            {
                                {
                                    "ErrorsHigh", new AlarmValues
                                                  (
                                                    evaluationPeriods : 3,
                                                    periodMinutes : 10,
                                                    value : 100
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

            ioc.GetMock<IAmazonLambda>().HasLambdaFunctions(new[]
            {
                new FunctionConfiguration  { FunctionName = "lambda-test", Timeout = 10 }
            });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();

            // act

            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            // assert

            var stack = cloudformation
                .Stack("Watchman-test");

            var alarmsByFunction = stack
                .AlarmsByDimension("FunctionName");

            Assert.That(alarmsByFunction.ContainsKey("lambda-test"), Is.True);
            var alarmsForLambda = alarmsByFunction["lambda-test"];

            Assert.That(alarmsForLambda.Exists(
                    alarm =>
                        alarm.Properties["MetricName"].Value<string>() == "Errors"
                        && alarm.Properties["Period"].Value<int>() == 60 * 10
                        && alarm.Properties["EvaluationPeriods"].Value<int>() == 3
                        && alarm.Properties["Threshold"].Value<int>() == 100)
            );
        }
    }
}
