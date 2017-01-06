using NUnit.Framework;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Dynamo;

namespace Watchman.Tests
{
    [TestFixture]
    public class IocTests
    {
        [Test]
        public void TheAlarmGeneratorResovles()
        {
            var container = new IocBootstrapper()
                .ConfigureContainer(ValidStartupParameters());

            var generator = container.GetInstance<DynamoAlarmGenerator>();

            Assert.That(generator, Is.Not.Null);
        }

        [Test]
        public void TheAlarmLoaderAndGeneratorResovles()
        {
            var container = new IocBootstrapper()
                .ConfigureContainer(ValidStartupParameters());

            var loader = container.GetInstance<AlarmLoaderAndGenerator>();

            Assert.That(loader, Is.Not.Null);
        }

        private static StartupParameters ValidStartupParameters()
        {
            return new StartupParameters
            {
                AwsAccessKey = "a",
                AwsSecretKey = "b",
                ConfigFolderLocation = "c:\\temp"
            };
        }
    }
}
