using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration.Load;
using Moq;
using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class ConfigFileLoaderThresholdTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            var testFilePath = TestFiles.GetRelativePathTo("data\\withThresholds");
            var testFilesSettings = new FileSettings(testFilePath);

            var logger = new Mock<IConfigLoadLogger>();
            var loader = new ConfigLoader(testFilesSettings, logger.Object);

            _config = loader.LoadConfig();
        }

        [Test]
        public void TheConfigIsNotNull()
        {
            Assert.That(_config, Is.Not.Null);
        }

        [Test]
        public void TheConfigIsNotEmpty()
        {
            Assert.That(_config.AlertingGroups.Count, Is.EqualTo(1));

            foreach (var group in _config.AlertingGroups)
            {
                AssertGroupDataIsLoaded(group);
            }
        }

        private static void AssertGroupDataIsLoaded(AlertingGroup alertingGroup)
        {
            Assert.That(alertingGroup.Name, Is.Not.Empty);
            Assert.That(alertingGroup.Targets, Is.Not.Empty);
            Assert.That(alertingGroup.AlarmNameSuffix, Is.Not.Empty);
            Assert.That(alertingGroup.IsCatchAll, Is.False);

            var alertEmail = (AlertEmail) alertingGroup.Targets.First();
            Assert.That(alertEmail.Email, Is.Not.Empty);
        }

        [Test]
        public void TheConfigContainsCorrectNames()
        {
            var names = _config.AlertingGroups
                .Select(g => g.Name)
                .ToList();

            AssertContainsName(names, "LambdaTest");
        }

        private static void AssertContainsName(List<string> names, string test)
        {
            Assert.That(names.Count(s => s == test), Is.EqualTo(1));
        }

        [Test]
        public void LambdaResourcesAreDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "LambdaTest");

            Assert.That(group, Is.Not.Null);
            var section = group.Services["Lambda"];

            Assert.That(section, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Empty);

            Assert.That(section.Resources, Is.Not.Null);
            Assert.That(section.Resources, Is.Not.Empty);
        }

        [Test]
        public void LambdaThresholdsAreDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "LambdaTest");

            Assert.That(group, Is.Not.Null);
            var section = group.Services["Lambda"];

            Assert.That(section, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Empty);

            Assert.That(section.Thresholds, Is.Not.Null);
            Assert.That(section.Thresholds, Is.Not.Empty);
        }

        [Test]
        public void LambdaThresholdsAreCorrect()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "LambdaTest");

            Assert.That(group, Is.Not.Null);
            var thresholds = group.Services["Lambda"].Thresholds;

            Assert.That(thresholds.Count, Is.EqualTo(3));

            var errrorsHigh = thresholds["ErrorsHigh"];
            var durationHigh = thresholds["DurationHigh"];
            var throttlesHigh = thresholds["ThrottlesHigh"];

            Assert.That(errrorsHigh.Value, Is.EqualTo(20));
            Assert.That(errrorsHigh.EvaluationPeriods, Is.EqualTo(2));

            Assert.That(durationHigh.Value, Is.EqualTo(30));
            Assert.That(durationHigh.EvaluationPeriods, Is.Null);

            Assert.That(throttlesHigh.Value, Is.EqualTo(40));
            Assert.That(throttlesHigh.EvaluationPeriods, Is.Null);
        }
    }
}
