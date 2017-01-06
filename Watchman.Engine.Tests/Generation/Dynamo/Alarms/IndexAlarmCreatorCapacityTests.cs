using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2.Model;
using Moq;
using NUnit.Framework;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Tests.Generation.Dynamo.Alarms
{
    [TestFixture]
    public class IndexAlarmCreatorCapacityTests
    {
        [Test]
        public async Task TestBasicReadCapacityAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task TestBasicWriteCapacityAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunReadCapacityAlarmIsNotPut()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunWriteCapacityAlarmIsNotPut()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 300);

            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 15600, 300);

            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object,logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300);

            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object,
                alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenReadCapacityAlarmExistsWithDifferentPeriodAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 31200, 123);

            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object,
                alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureReadCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteCapacityAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 42, 300);

            var logger = new Mock<IAlarmLogger>();

            var indexAlarmCreator = new IndexAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();
            var index = MakeIndexDescription();

            await indexAlarmCreator.EnsureWriteCapacityAlarm(table, index, "suffix", 0.52, "testArn", false);

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
