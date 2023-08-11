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
    public class IndexAlarmCreatorCapacityTests
    {
        [Test]
        public async Task TestBasicReadCapacityAlarmCreation()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task TestBasicWriteCapacityAlarmCreation()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunReadCapacityAlarmIsNotPut()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunWriteCapacityAlarmIsNotPut()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 15600, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder,logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch,
                alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentPeriodAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 123, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch,
                alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 15600, 300, "firstTarget");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 15600, 300, "firstTarget");

            var logger = Substitute.For<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        private static TableDescription MakeTableDescription()
        {
            return  new TableDescription
                {
                    TableName = "testTable"
                };
        }

        private static GlobalSecondaryIndexDescription MakeIndexDescription()
        {
            return new GlobalSecondaryIndexDescription
            {
                IndexName = "testIndex",
                ProvisionedThroughput = new ProvisionedThroughputDescription
                {
                    ReadCapacityUnits = 200,
                    WriteCapacityUnits = 100
                }
            };
        }
    }
}
