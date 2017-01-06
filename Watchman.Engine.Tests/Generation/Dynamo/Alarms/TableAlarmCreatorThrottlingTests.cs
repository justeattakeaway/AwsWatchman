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
    public class TableAlarmCreatorThrottlingTests
    {
        [Test]
        public async Task TestBasicReadThrottlingAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadThrottleAlarm(table, "suffix", 10, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task TestBasicWriteThrottlingAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteThrottleAlarm(table, "suffix", 10, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunReadThrottlingAlarmIsNotPut()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadThrottleAlarm(table, "suffix", 10, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunWriteThrottlingAlarmIsNotPut()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteThrottleAlarm(table, "suffix", 10, "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadThrottlingAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 5, 60);

            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadThrottleAlarm(table, "suffix", 5, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenWriteThrottlingAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 5, 60);

            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteThrottleAlarm(table, "suffix", 5, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenReadThrottlingAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 101, 300);

            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureReadThrottleAlarm(table, "suffix", 10, "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenWriteThrottlingAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 101, 300);

            var logger = new Mock<IAlarmLogger>();

            var tableAlarmCreator = new TableAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            var table = MakeTableDescription();

            await tableAlarmCreator.EnsureWriteThrottleAlarm(table, "suffix", 10, "testArn", false);

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
