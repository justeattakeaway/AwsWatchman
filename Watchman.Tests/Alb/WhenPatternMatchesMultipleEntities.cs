using NUnit.Framework;

namespace Watchman.Tests.Alb
{
    public class WhenPatternMatchesMultipleEntities
    {
        private AlbTestSetupData _albTestSetupData;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _albTestSetupData = await new AlbTestSetupBuilder()
                .WithLoadBalancer("loadbalancer1", "loadbalancer/arn1")
                .WithLoadBalancer("loadbalancer2", "loadbalancer/arn2")
                .WithPattern("loadBalancer")
                .Build();
        }

        [Test]
        public void ThenCloudFormationStackShouldBeDeployed()
        {
            Assert.That(_albTestSetupData.FakeCloudFormation.StacksDeployed, Is.EqualTo(1));
        }

        [Test]
        public void ThenDefaultAlarmsShouldBeAddedForEachMatch()
        {
            Assert.That(_albTestSetupData.Alarms, Is.Not.Null);
            Assert.That(_albTestSetupData.Alarms.Count, Is.EqualTo(8));

            var alarmsForLoadBalancer1 = _albTestSetupData.Alarms.Where(x =>
                x.GetPropertyValue("AlarmName").Contains(_albTestSetupData.LoadBalancers[0].LoadBalancerName));

            var alarmsForLoadBalancer2 = _albTestSetupData.Alarms.Where(x =>
                x.GetPropertyValue("AlarmName").Contains(_albTestSetupData.LoadBalancers[1].LoadBalancerName));

            Assert.That(alarmsForLoadBalancer1.Count, Is.EqualTo(4));
            Assert.That(alarmsForLoadBalancer2.Count, Is.EqualTo(4));
        }
    }
 }
