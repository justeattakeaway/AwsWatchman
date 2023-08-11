using Amazon.CloudWatch;
using NSubstitute;
using NUnit.Framework;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.LegacyTracking;
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
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenDryRunQueueLengthAlarmIsNotPut()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", true);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenOldestMessageAlarmExistsAtSameLevelNoAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 100,
                AwsConstants.FiveMinutesInSeconds, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasNotCalled(cloudWatch);
        }

        [Test]
        public async Task WhenOldestMessageAlarmExistsWithDifferentThresholdAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 101,
                AwsConstants.FiveMinutesInSeconds, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenOldestMessageAlarmExistsWithDifferentPeriodAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 100,
                AwsConstants.FiveMinutesInSeconds + 1, "testArn");

            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "testArn", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }

        [Test]
        public async Task WhenOldestMessageAlarmExistsWithDifferentTargetAlarmIsCreated()
        {
            var cloudWatch = Substitute.For<IAmazonCloudWatch>();
            var alarmFinder = Substitute.For<IAlarmFinder>();
            VerifyCloudwatch.AlarmFinderFindsThreshold(alarmFinder, 100,
                AwsConstants.FiveMinutesInSeconds, "firstTarget");

            var logger = Substitute.For<IAlarmLogger>();

            var queueAlarmCreator = new QueueAlarmCreator(
                cloudWatch, alarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            await queueAlarmCreator.EnsureOldestMessageAlarm("testQueue", 100, "suffix", "secondTarget", false);

            VerifyCloudwatch.PutMetricAlarmWasCalledOnce(cloudWatch);
        }
    }
}
