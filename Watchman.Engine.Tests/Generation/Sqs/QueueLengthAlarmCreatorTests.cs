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
    public class QueueLengthAlarmCreatorTests
    {
        [Test]
        public async Task TestBasicQueueLengthAlarmCreation()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "testArn", false);

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

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 10,
                AwsConstants.FiveMinutesInSeconds, "testArn");

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 101,
                AwsConstants.FiveMinutesInSeconds, "testArn");

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsWithDifferentPeriodAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 10,
                AwsConstants.FiveMinutesInSeconds + 1, "testArn");

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenQueueLengthAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = new Mock<IAmazonCloudWatch>();
            var alarmFinder = new Mock<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 10,
                AwsConstants.FiveMinutesInSeconds, "firstTarget");

            var logger = new Mock<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch.Object, alarmFinder.Object, logger.Object);

            await queueAlarmCreator.EnsureLengthAlarm("testQueue", 10, "suffix", "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }
    }
}
