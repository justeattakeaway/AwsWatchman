using NSubstitute;
using NUnit.Framework;
using Watchman.Configuration.Load;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class ConfigFileLoaderThresholdTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            var testFilePath = TestFiles.GetRelativePathTo(Path.Join("data", "withThresholds"));
            var testFilesSettings = new FileSettings(testFilePath);

            var logger = Substitute.For<IConfigLoadLogger>();
            var loader = new ConfigLoader(testFilesSettings, logger);

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
            var section = group.Services.Lambda;

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
            var section = group.Services.Lambda;

            Assert.That(section, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Empty);

            Assert.That(section.Values, Is.Not.Null);
            Assert.That(section.Values, Is.Not.Empty);
            Assert.That(section.Values.Count, Is.EqualTo(9));

            Assert.That(section.Values["ThrottlesHigh"].Threshold, Is.EqualTo(40));
            Assert.That(section.Values["FloatValue"].Threshold, Is.EqualTo(2.1));
            Assert.That(section.Values["FloatValueAsString"].Threshold, Is.EqualTo(2.2));
            Assert.That(section.Values["IntValueAsString"].Threshold, Is.EqualTo(41));
            Assert.That(section.Values["InvocationsLow"].Threshold, Is.EqualTo(5));
            Assert.That(section.Values["InvocationsHigh"].Threshold, Is.EqualTo(10));
        }

        [Test]
        public void LambdaValuesAreCorrect()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "LambdaTest");

            Assert.That(group, Is.Not.Null);
            var values = group.Services.Lambda.Values;

            var errorsHigh = values["ErrorsHigh"];
            var durationHigh = values["DurationHigh"];
            var throttlesHigh = values["ThrottlesHigh"];
            var invocationsLow = values["InvocationsLow"];
            var fooHigh = values["FooHigh"];

            Assert.That(errorsHigh.Threshold, Is.EqualTo(20));
            Assert.That(errorsHigh.EvaluationPeriods, Is.EqualTo(2));
            Assert.That(errorsHigh.PeriodMinutes, Is.EqualTo(10));

            Assert.That(durationHigh.Threshold, Is.EqualTo(30));
            Assert.That(durationHigh.EvaluationPeriods, Is.Null);
            Assert.That(durationHigh.PeriodMinutes, Is.Null);

            Assert.That(throttlesHigh.Threshold, Is.EqualTo(40));
            Assert.That(throttlesHigh.EvaluationPeriods, Is.Null);
            Assert.That(throttlesHigh.PeriodMinutes, Is.Null);

            Assert.That(fooHigh.Threshold, Is.Null);
            Assert.That(fooHigh.EvaluationPeriods, Is.EqualTo(3));
            Assert.That(fooHigh.PeriodMinutes, Is.Null);

            Assert.That(invocationsLow.Threshold, Is.EqualTo(5));
            Assert.That(invocationsLow.EvaluationPeriods, Is.Null);
            Assert.That(invocationsLow.PeriodMinutes, Is.Null);
        }
    }
}
