using Amazon.CloudWatch;
using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.LegacyTracking;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Tests.Generation.Dynamo.Alarms
{
    [TestFixture]
    public class TableAlarmCreatorCapacityTests
    {
        [Test]
        public async Task TestBasicReadCapacityAlarmCreation()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task TestBasicWriteCapacityAlarmCreation()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunReadCapacityAlarmIsNotPut()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunWriteCapacityAlarmIsNotPut()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteCapacityAlarm(table, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 15600, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 300, "firstTarget");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteCapacityAlarm(table, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 300, "firstTarget");

            var logger = Substitute.For<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadCapacityAlarm(table, "suffix", 0.52, "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        private static TableDescription MakeTableDescription()
        {
            return new TableDescription
            {
                TableName = "testTable",
                ProvisionedThroughput = new ProvisionedThroughputDescription
                {
                    ReadCapacityUnits = 200,
                    WriteCapacityUnits = 100
                }
            };
        }
    }
}
