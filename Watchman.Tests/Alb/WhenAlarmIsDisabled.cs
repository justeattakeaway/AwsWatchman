using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Tests.Alb
{
    public class WhenAlarmIsDisabled
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder()
                .WithOverride("Target5xxErrorsHigh", new AlarmValues(null, null, null, null, false))
                .Build();
        }

        [Test]
        public void ThenAlarmShouldNotExist()
        {
            var alarmName = $"{_albTestSetupData.LoadBalancers.First().LoadBalancerName}-Target5xxErrorsHigh-{_albTestSetupData.ConfigurationSuffix}";
            var alarm = _albTestSetupData.Alarms.FirstOrDefault(x => x.GetPropertyValue("AlarmName") == alarmName);

            Assert.That(alarm, Is.Null);
        }

        [Test]
        public void ThenTheRestOfTheAlarmsShouldExist()
        {
            Assert.That(_albTestSetupData.Alarms, Is.Not.Null);
            Assert.That(_albTestSetupData.Alarms.Count, Is.EqualTo(3));
        }
    }
}
