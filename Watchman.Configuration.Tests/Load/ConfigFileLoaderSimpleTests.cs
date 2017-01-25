using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration.Load;
using Moq;
using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class ConfigFileLoaderSimpleTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            var testFilePath = TestFiles.GetPathTo("simpleData");
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
            Assert.That(_config.AlertingGroups.Count, Is.EqualTo(5));

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

            AssertContainsName(names, "AutoscalingOnly");
            AssertContainsName(names, "DynamoOnly");
            AssertContainsName(names, "LambdaOnly");
            AssertContainsName(names, "RdsOnly");
            AssertContainsName(names, "SqsOnly");
        }

        private static void AssertContainsName(List<string> names, string test)
        {
            Assert.That(names.Count(s => s == test), Is.EqualTo(1));
        }

        [Test]
        public void DynamoSimpleTestDynamoDataIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoOnly");

            Assert.That(group, Is.Not.Null);
            Assert.That(group.DynamoDb.Threshold, Is.Null);
            Assert.That(group.DynamoDb.MonitorThrottling, Is.Null);
        }

        [Test]
        public void DynamoSimpleTestTargetsAreDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoOnly");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.Targets[0], Is.InstanceOf<AlertEmail>());
            Assert.That(group.Targets[1], Is.InstanceOf<AlertUrl>());

            var alertEmail = (AlertEmail)group.Targets[0];
            var alertUrl = (AlertUrl)group.Targets[1];

            Assert.That(alertEmail.Email, Is.EqualTo("DynamoOnly@example.com"));
            Assert.That(alertUrl.Url, Is.EqualTo("http://farley.com"));
        }

        [Test]
        public void DynamoSimpleTestDynamoTablesAreDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoOnly");

            Assert.That(group, Is.Not.Null);
            Assert.That(group.DynamoDb.Threshold, Is.Null);

            Assert.That(group.DynamoDb.Tables.Count, Is.EqualTo(1));
            Assert.That(group.DynamoDb.Tables[0].Name, Is.EqualTo("test-table"));
        }

        [Test]
        public void SqsOnlyGroupIsDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "SqsOnly");

            Assert.That(group, Is.Not.Null);

            var queues = group.Sqs.Queues;

            Assert.That(queues[0].Name, Is.EqualTo("queue1"));
            Assert.That(queues[0].Pattern, Is.Null);

            Assert.That(queues[1].Name, Is.Null);
            Assert.That(queues[1].Pattern, Is.EqualTo("foo"));
        }

        [Test]
        public void AutoScalingResourcesAreDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "AutoscalingOnly");

            Assert.That(group, Is.Not.Null);
            AssertSectionIsPopulated(group.Services["AutoScaling"]);
        }

        [Test]
        public void LambdaResourcesAreDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "LambdaOnly");

            Assert.That(group, Is.Not.Null);
            AssertSectionIsPopulated(group.Services["Lambda"]);
        }

        [Test]
        public void RdsResourcesAreDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "RdsOnly");

            Assert.That(group, Is.Not.Null);
            AssertSectionIsPopulated(group.Services["Rds"]);
        }

        private static void AssertSectionIsPopulated(AwsServiceAlarms section)
        {
            Assert.That(section, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Null);
            Assert.That(section.ExcludeResourcesPrefixedWith, Is.Not.Empty);

            Assert.That(section.Resources, Is.Not.Null);
            Assert.That(section.Resources, Is.Not.Empty);
        }
    }
}
