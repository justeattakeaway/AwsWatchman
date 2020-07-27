using Amazon.CloudWatch;
using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Tests
{
    [TestFixture]
    public class AlarmDefinitionTests
    {
        [Test]
        public void TestDefaultFlagsState()
        {
            var alarmDef = new AlarmDefinition();

            Assert.That(alarmDef.AlertOnInsufficientData, Is.False);
            Assert.That(alarmDef.AlertOnOk, Is.True);
        }

        [Test]
        public void TestFlagsCopy()
        {
            var original = new AlarmDefinition
                {
                    AlertOnInsufficientData = true,
                    AlertOnOk = false
                };

            var copy = original.Copy();

            Assert.That(copy.AlertOnInsufficientData, Is.True);
            Assert.That(copy.AlertOnOk, Is.False);
        }

        [Test]
        [TestCaseSource("_validStatistics")]
        public void TestCopyWithValidStatistic(Statistic originalStatistic, Statistic newStatistic)
        {
            var original = new AlarmDefinition
            {
                Statistic = originalStatistic
            };

            var copy = original.CopyWith(null, new AlarmValues(statistic: newStatistic.Value));

            Assert.That(copy.Statistic, Is.EqualTo(newStatistic));
        }

        [Test]
        public void TestCopyWithInvalidStatistic()
        {
            var original = new AlarmDefinition
            {
                Statistic = Statistic.Maximum
            };

            var copy = original.CopyWith(null, new AlarmValues(statistic: "ynwa"));

            Assert.That(copy.Statistic, Is.EqualTo(Statistic.Maximum));
        }

        private static object[] _validStatistics =
        {
            new object[] {  Statistic.Maximum, Statistic.Average},
            new object[] {  Statistic.Average, Statistic.Maximum},
            new object[] {  Statistic.Maximum, Statistic.Minimum},
            new object[] {  Statistic.Maximum, Statistic.SampleCount},
            new object[] {  Statistic.Maximum, Statistic.Sum}
        };
    }
}
