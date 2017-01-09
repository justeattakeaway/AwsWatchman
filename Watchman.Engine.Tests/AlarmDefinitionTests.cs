using NUnit.Framework;

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
    }
}
