using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration.Load;
using Moq;
using NUnit.Framework;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class ConfigFileLoaderTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            var testFilePath = TestFiles.GetPathTo("data");
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

            var alertEmail = (AlertEmail)alertingGroup.Targets.First();
            Assert.That(alertEmail.Email, Is.Not.Empty);
        }

        [Test]
        public void TheConfigContainsCorrectNames()
        {
            var names = _config.AlertingGroups
                .Select(g => g.Name)
                .ToList();

            AssertContainsName(names, "DynamoAndSqsTest");
            AssertContainsName(names, "DynamoGroup2");
            AssertContainsName(names, "SqsOptionsTest");
            AssertContainsName(names, "TablePatternGroup");
            AssertContainsName(names, "TablePatternGroup2");
        }

        private static void AssertContainsName(List<string> names, string test)
        {
            Assert.That(names.Count(s => s == test), Is.EqualTo(1));
        }

        [Test]
        public void DynamoGroup2DataIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoGroup2");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.Name, Is.EqualTo("DynamoGroup2"));
            Assert.That(group.AlarmNameSuffix, Is.EqualTo("DynamoGroup2"));
            Assert.That(group.IsCatchAll, Is.False);

            var alertEmail = (AlertEmail)group.Targets[0];
            Assert.That(alertEmail.Email, Is.EqualTo("DynamoGroup2@example.com"));
        }

        [Test]
        public void DynamoGroup2DynamoDataIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoGroup2");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.DynamoDb.Threshold, Is.EqualTo(0.75));
            Assert.That(group.DynamoDb.MonitorThrottling, Is.Null);
        }

        [Test]
        public void DynamoGroup2TablesAreDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoGroup2");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.DynamoDb.Tables, Is.Not.Null);

            var tables = group.DynamoDb.Tables;

            Assert.That(tables.Count, Is.EqualTo(2));
            Assert.That(tables[0].Name, Is.EqualTo("test-data"));
            Assert.That(tables[0].Threshold, Is.Null);
            Assert.That(tables[0].MonitorWrites, Is.Null);

            Assert.That(tables[1].Name, Is.EqualTo("test-keys"));
            Assert.That(tables[1].Threshold, Is.EqualTo(0.5));
            Assert.That(tables[1].MonitorWrites, Is.Null);
        }

        [Test]
        public void DynamoGroup2ExclusionsAreDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoGroup2");

            Assert.That(group, Is.Not.Null);
            Assert.That(group.DynamoDb.ExcludeTablesPrefixedWith, Is.Not.Null);

            Assert.That(group.DynamoDb.ExcludeTablesPrefixedWith.Count, Is.EqualTo(1));
            Assert.That(group.DynamoDb.ExcludeTablesPrefixedWith[0], Is.EqualTo("exclude_all"));

            Assert.That(group.DynamoDb.ExcludeReadsForTablesPrefixedWith.Count, Is.EqualTo(1));
            Assert.That(group.DynamoDb.ExcludeReadsForTablesPrefixedWith[0], Is.EqualTo("exclude_read"));

            Assert.That(group.DynamoDb.ExcludeWritesForTablesPrefixedWith.Count, Is.EqualTo(1));
            Assert.That(group.DynamoDb.ExcludeWritesForTablesPrefixedWith[0], Is.EqualTo("exclude_write"));
        }

        [Test]
        public void TablePatternGroupIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "TablePatternGroup");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.Name, Is.EqualTo("TablePatternGroup"));

            var alertEmail = (AlertEmail)group.Targets[0];

            Assert.That(alertEmail.Email, Is.EqualTo("TablePatternGroup@example.com"));
            Assert.That(group.AlarmNameSuffix, Is.EqualTo("TablePatternGroup"));
        }

        [Test]
        public void TablePatternGroupFlagsAndExclusionsAreDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "TablePatternGroup");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.IsCatchAll, Is.True);
        }

        [Test]
        public void TablePatternGroupDynamoDataIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "TablePatternGroup");

            Assert.That(group, Is.Not.Null);

            Assert.That(group.DynamoDb.MonitorThrottling, Is.Null);
            Assert.That(group.DynamoDb.ExcludeTablesPrefixedWith[0], Is.EqualTo("group3-excluded"));
        }

        [Test]
        public void DynamoThottlingDataIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "TablePatternGroup2");

            Assert.That(group, Is.Not.Null);
            Assert.That(group.DynamoDb.MonitorThrottling.HasValue, Is.True);
            Assert.That(group.DynamoDb.MonitorThrottling.Value, Is.True);

            Assert.That(group.DynamoDb.ThrottlingThreshold.HasValue, Is.True);
            Assert.That(group.DynamoDb.ThrottlingThreshold.Value, Is.EqualTo(10));

        }

        [Test]
        public void TablePatternGroup2TablePatternIsDeserialized()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "TablePatternGroup2");

            Assert.That(group, Is.Not.Null);

            var tables = group.DynamoDb.Tables;

            Assert.That(tables[0].Pattern, Is.EqualTo("fish"));
            Assert.That(tables[0].Threshold, Is.EqualTo(0.45));
            Assert.That(tables[0].MonitorWrites, Is.False);
        }

        [Test]
        public void QueueSpecWithAllOptionsIsDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "SqsOptionsTest");

            Assert.That(group, Is.Not.Null);
            var allTheOptions = group.Sqs.Queues.FirstOrDefault(q => q.Pattern == "alltheoptions");

            Assert.That(allTheOptions, Is.Not.Null);
            Assert.That(allTheOptions.Name, Is.Null);
            Assert.That(allTheOptions.Pattern, Is.EqualTo("alltheoptions"));
            Assert.That(allTheOptions.LengthThreshold, Is.EqualTo(100));
            Assert.That(allTheOptions.OldestMessageThreshold, Is.EqualTo(600));

            Assert.That(allTheOptions.Errors, Is.Not.Null);
            Assert.That(allTheOptions.Errors.Monitored, Is.True);
            Assert.That(allTheOptions.Errors.LengthThreshold, Is.EqualTo(12));
        }

        [Test]
        public void DynamoAndSqsGroupIsDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoAndSqsTest");

            Assert.That(group, Is.Not.Null);

            var tables = group.DynamoDb.Tables;
            Assert.That(tables.Count, Is.EqualTo(2));

            var queues = group.Sqs.Queues;
            Assert.That(queues.Count, Is.EqualTo(2));

            Assert.That(queues[0].LengthThreshold, Is.Null);
            Assert.That(queues[1].LengthThreshold, Is.EqualTo(42));
        }

        [Test]
        public void SqsGroupThresholdIsDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoAndSqsTest");

            Assert.That(group, Is.Not.Null);
            Assert.That(group.Sqs, Is.Not.Null);
            Assert.That(group.Sqs.LengthThreshold, Is.Not.Null);
            Assert.That(group.Sqs.LengthThreshold, Is.EqualTo(57));

            Assert.That(group.Sqs.Errors, Is.Not.Null);
            Assert.That(group.Sqs.Errors.LengthThreshold, Is.Not.Null);
            Assert.That(group.Sqs.Errors.LengthThreshold, Is.EqualTo(13));
        }

        [Test]
        public void SqsQueueThresholdIsDeserialised()
        {
            var group = _config.AlertingGroups.FirstOrDefault(g => g.Name == "DynamoAndSqsTest");

            Assert.That(group, Is.Not.Null);
            var queues = group.Sqs.Queues;

            Assert.That(queues[0].LengthThreshold, Is.Null);
            Assert.That(queues[1].LengthThreshold, Is.EqualTo(42));

            Assert.That(queues[0].Errors, Is.Null);

            Assert.That(queues[1].Errors, Is.Not.Null);
            Assert.That(queues[1].Errors.LengthThreshold, Is.EqualTo(7));
        }
    }
}
