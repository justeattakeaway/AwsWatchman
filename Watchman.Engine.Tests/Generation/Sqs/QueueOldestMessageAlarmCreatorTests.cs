using System.Threading.Tasks;
using Amazon.CloudWatch;
using Moq;
using NUnit.Framework;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;
using Watchman.Engine.Tests.Generation.Dynamo.Alarms;

namespace Watchman.Engine.Tests.Generation.Sqs
{
    [TestFixture]
    public class QueueOldestMessageAlarmCreatorTests
    {
        [Test]
        public async Task TestBasicQueueOldestMessageAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunQueueLengthAlarmIsNotPut()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 100,
                AwsConstants.FiveMinutesInSeconds);

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 101,
                AwsConstants.FiveMinutesInSeconds);

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsWithDifferentPeriodAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 100,
                AwsConstants.FiveMinutesInSeconds + 1);

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

    }
}
