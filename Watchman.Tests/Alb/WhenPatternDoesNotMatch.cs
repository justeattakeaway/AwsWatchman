using NUnit.Framework;

namespace Watchman.Tests.Alb
{
    public class WhenPatternDoesNotMatch
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder()
                .WithPattern("notMatchingPattern")
                .Build();
        }

        [Test]
        public void ThenCloudFormationStackShouldNotBeDeployed()
        {
            Assert.That(_albTestSetupData.FakeCloudFormation.StacksDeployed, Is.Zero);
        }

        [Test]
        public void ThenTheDefaultAlarmsShouldNotExist()
        {
            Assert.That(_albTestSetupData.Alarms, Is.Null);
        }
    }
 }
