using Moq;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Engine.Generation.Sqs;

namespace Watchman.Engine.Tests.Generation.Sqs
{
    public static class VerifyQueues
    {
        public static void ReturnsQueues(Mock<IResourceSource<QueueData>> queueSource, List<string> queueNames)
        {
            queueSource.Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(queueNames);
        }

        public static void EnsureLengthAlarm(Mock<IQueueAlarmCreator> alarmCreator,
            string queueName, bool isDryRun)
        {
            alarmCreator.Verify(x => x.EnsureLengthAlarm(queueName,
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), isDryRun), Times.Once);
        }

        public static void EnsureLengthAlarm(Mock<IQueueAlarmCreator> alarmCreator,
            string queueName, int threshold, bool isDryRun)
        {
            alarmCreator.Verify(x => x.EnsureLengthAlarm(queueName,
                threshold, It.IsAny<string>(), It.IsAny<string>(), isDryRun), Times.Once);
        }

        public static void EnsureOldestMessageAlarm(Mock<IQueueAlarmCreator> alarmCreator,
          string queueName, int threshold, bool isDryRun)
        {
            alarmCreator.Verify(x => x.EnsureOldestMessageAlarm(queueName,
                threshold, It.IsAny<string>(), It.IsAny<string>(), isDryRun), Times.Once);
        }

        public static void NoLengthAlarm(Mock<IQueueAlarmCreator> alarmCreator, string queueName)
        {
            alarmCreator.Verify(x => x.EnsureLengthAlarm(queueName,
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        public static void NoOldestMessageAlarm(Mock<IQueueAlarmCreator> alarmCreator, string queueName)
        {
            alarmCreator.Verify(x => x.EnsureOldestMessageAlarm(queueName,
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
