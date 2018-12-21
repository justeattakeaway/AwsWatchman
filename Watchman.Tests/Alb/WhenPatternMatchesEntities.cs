using System.Threading.Tasks;
using NUnit.Framework;

namespace Watchman.Tests.Alb
{
    public class WhenPatternMatchesEntities
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder()
                .WithPattern("loadBalancer")
                .Build();
        }

        [Test]
        public void ThenCloudFormationStackShouldBeDeployed()
        {
            Assert.That(_albTestSetupData.FakeCloudFormation.StacksDeployed, Is.EqualTo(1));
        }

        [Test]
        public void ThenTheDefaultAlarmsShouldExist()
        {
            Assert.That(_albTestSetupData.Alarms, Is.Not.Null);
            Assert.That(_albTestSetupData.Alarms.Count, Is.EqualTo(4));
        }
    }
 }
