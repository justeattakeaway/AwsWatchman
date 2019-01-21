using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Tests.Alb
{
    public class WhenAlarmIsCreatedWithOverrides
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder()
                .WithOverride("TargetResponseTimeHigh", new AlarmValues(20, 2, "p95"))
                .WithOverride("RejectedConnectionCountHigh", new AlarmValues(25, 3, "p90"))
                .Build();
        }

        [Test]
        public void ThenThe5xxErrorsHighAlarmShouldHaveTheDefaultProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-5xxErrorsHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("5"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("10"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.EqualTo("Sum"));
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.Empty);
        }

        [Test]
        public void ThenTheTarget5xxErrorsHighAlarmShouldHaveTheDefaultProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-Target5xxErrorsHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("5"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("10"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.EqualTo("Sum"));
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.Empty);
        }

        [Test]
        public void ThenTheRejectedConnectionCountHighAlarmShouldHaveOverriddenProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-RejectedConnectionCountHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("3"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("25"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.Empty);
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.EqualTo("p90"));
        }

        [Test]
        public void ThenTheTargetResponseTimeHighAlarmShouldHaveOverriddenProperties()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-TargetResponseTimeHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Not.Null);
            Assert.That(alarm.GetPropertyValue("EvaluationPeriods"), Is.EqualTo("2"));
            Assert.That(alarm.GetPropertyValue("Threshold"), Is.EqualTo("20"));
            Assert.That(alarm.GetPropertyValue("Statistic"), Is.Empty);
            Assert.That(alarm.GetPropertyValue("ExtendedStatistic"), Is.EqualTo("p95"));
        }
    }
 }
