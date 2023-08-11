using NSubstitute;
using NUnit.Framework;
using Watchman.Configuration.Load;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class CustomParametersTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            var testFilePath = TestFiles.GetRelativePathTo("data");
            var testFilesSettings = new FileSettings(testFilePath);

            var logger = Substitute.For<IConfigLoadLogger>();
            var loader = new ConfigLoader(testFilesSettings, logger);

            _config = loader.LoadConfig();
        }

        [Test]
        public void TheConfigIsNotNull()
        {
            Assert.That(_config, Is.Not.Null);

            var customConfig = _config.AlertingGroups.FirstOrDefault(ag => ag.Name == "CustomParametersTest");

            Assert.That(customConfig, Is.Not.Null);

            var autoscaling = customConfig.Services.AutoScaling;

            Assert.That(autoscaling.Options.InstanceCountIncreaseDelayMinutes, Is.EqualTo(50));

            var resourceWithParameters = autoscaling.Resources.SingleOrDefault(r => r.Options != null);

            Assert.That(resourceWithParameters, Is.Not.Null);
            Assert.That(resourceWithParameters.Options.InstanceCountIncreaseDelayMinutes, Is.EqualTo(20));
        }
    }
}
