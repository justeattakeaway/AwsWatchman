using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using NUnit.Framework;
using TestHelper;
using Watchman.Engine.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.IntegrationTests
{
    [TestFixture]
    public class AlarmFinderTests
    {
        private readonly IAlarmFinder _finder = MakeAlarmFinder();

        [Test]
        public async Task TestGet()
        {
            var alarm = await _finder.FindAlarmByName("no_such_alarm_1345345");

            Assert.That(alarm, Is.Null);
        }

        [Test]
        public async Task TestCount()
        {
            await _finder.FindAlarmByName("dummy");
            var count = _finder.Count;

            Assert.That(count, Is.GreaterThan(0));
        }

        private static IAlarmFinder MakeAlarmFinder()
        {
            var cloudwatch = new AmazonCloudWatchClient(CredentialsReader.GetCredentials(),
                new AmazonCloudWatchConfig {RegionEndpoint = RegionEndpoint.EUWest1});
            return new AlarmFinder(new ConsoleAlarmLogger(false), cloudwatch);
        }

    }
}
