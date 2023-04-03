﻿using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.Engine;

namespace Watchman.Tests.Alb
{
    public class WhenAlarmsAreCreatedWithDefaults
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder().Build();
        }

        [Test]
        public void ThenTheDefaultAlarmsShouldExist()
        {
            Assert.That(_albTestSetupData.Alarms, Is.Not.Null);
            Assert.That(_albTestSetupData.Alarms.Count, Is.EqualTo(4));
        }

        [Test]
        public void ThenThe5xxErrorsHighAlarmShouldHaveCorrectProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-5xxErrorsHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("AlarmDescription"),
                Is.EqualTo("Alarm (new version) managed by AwsWatchman. Alerting group: test"));
            Assert.That(alarm.GetPropertyValue("Namespace"), Is.EqualTo(AwsNamespace.Alb));
            Assert.That(alarm.GetPropertyValue("MetricName"), Is.EqualTo("HTTPCode_ELB_5XX_Count"));
            Assert.That(alarm.Properties["Dimensions"].First["Name"].Value<string>(), Is.EqualTo("LoadBalancer"));
            Assert.That(alarm.Properties["Dimensions"].First["Value"].Value<string>(), Is.EqualTo("loadBalancer-1"));
            Assert.That(alarm.GetPropertyValue("ComparisonOperator"), Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Period"), Is.EqualTo("60"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("10"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.EqualTo("Sum"));
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.Empty);
        }

        [Test]
        public void ThenTheTarget5xxErrorsHighAlarmShouldHaveCorrectProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-Target5xxErrorsHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("AlarmDescription"),
                Is.EqualTo("Alarm (new version) managed by AwsWatchman. Alerting group: test"));
            Assert.That(alarm.GetPropertyValue("Namespace"), Is.EqualTo(AwsNamespace.Alb));
            Assert.That(alarm.GetPropertyValue("MetricName"), Is.EqualTo("HTTPCode_Target_5XX_Count"));
            Assert.That(alarm.Properties["Dimensions"].First["Name"].Value<string>(), Is.EqualTo("LoadBalancer"));
            Assert.That(alarm.Properties["Dimensions"].First["Value"].Value<string>(), Is.EqualTo("loadBalancer-1"));
            Assert.That(alarm.GetPropertyValue("ComparisonOperator"), Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Period"), Is.EqualTo("60"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("10"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.EqualTo("Sum"));
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.Empty);
        }

        [Test]
        public void ThenTheRejectedConnectionCountHighAlarmShouldHaveCorrectProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-RejectedConnectionCountHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("AlarmDescription"),
                Is.EqualTo("Alarm (new version) managed by AwsWatchman. Alerting group: test"));
            Assert.That(alarm.GetPropertyValue("Namespace"), Is.EqualTo(AwsNamespace.Alb));
            Assert.That(alarm.GetPropertyValue("MetricName"), Is.EqualTo("RejectedConnectionCount"));
            Assert.That(alarm.Properties["Dimensions"].First["Name"].Value<string>(), Is.EqualTo("LoadBalancer"));
            Assert.That(alarm.Properties["Dimensions"].First["Value"].Value<string>(), Is.EqualTo("loadBalancer-1"));
            Assert.That(alarm.GetPropertyValue("ComparisonOperator"), Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Period"), Is.EqualTo("60"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("10"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.EqualTo("Sum"));
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.Empty);
        }

        [Test]
        public void ThenTheTargetResponseTimeHighAlarmShouldHaveCorrectProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-TargetResponseTimeHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("AlarmDescription"),
                Is.EqualTo("Alarm (new version) managed by AwsWatchman. Alerting group: test"));
            Assert.That(alarm.GetPropertyValue("Namespace"), Is.EqualTo(AwsNamespace.Alb));
            Assert.That(alarm.GetPropertyValue("MetricName"), Is.EqualTo("TargetResponseTime"));
            Assert.That(alarm.Properties["Dimensions"].First["Name"].Value<string>(), Is.EqualTo("LoadBalancer"));
            Assert.That(alarm.Properties["Dimensions"].First["Value"].Value<string>(), Is.EqualTo("loadBalancer-1"));
            Assert.That(alarm.GetPropertyValue("ComparisonOperator"), Is.EqualTo("GreaterThanOrEqualToThreshold"));
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Period"), Is.EqualTo("60"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.Empty);
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.EqualTo("p99"));
        }
    }
 }
