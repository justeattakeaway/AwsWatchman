using NSubstitute;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Engine.Generation.Sqs;

namespace Watchman.Engine.Tests.Generation.Sqs
{
    public static class VerifyQueues
    {
        public static void ReturnsQueues(IResourceSource<QueueData> queueSource, List<string> queueNames)
        {
            queueSource
                .GetResourceNamesAsync()
                .Returns(queueNames);
        }

        public static void EnsureLengthAlarm(IQueueAlarmCreator alarmCreator,
            string queueName, bool isDryRun)
        {
            alarmCreator.Received(1).EnsureLengthAlarm(queueName,
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), isDryRun);
        }

        public static void EnsureLengthAlarm(IQueueAlarmCreator alarmCreator,
            string queueName, int threshold, bool isDryRun)
        {
            alarmCreator.Received(1).EnsureLengthAlarm(queueName,
                threshold, Arg.Any<string>(), Arg.Any<string>(), isDryRun);
        }

        public static void EnsureOldestMessageAlarm(IQueueAlarmCreator alarmCreator,
          string queueName, int threshold, bool isDryRun)
        {
            alarmCreator.Received(1).EnsureOldestMessageAlarm(queueName,
                threshold, Arg.Any<string>(), Arg.Any<string>(), isDryRun);
        }

        public static void NoLengthAlarm(IQueueAlarmCreator alarmCreator, string queueName)
        {
            alarmCreator.DidNotReceive().EnsureLengthAlarm(queueName,
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        }

        public static void NoOldestMessageAlarm(IQueueAlarmCreator alarmCreator, string queueName)
        {
            alarmCreator.DidNotReceive().EnsureOldestMessageAlarm(queueName,
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        }
    }
}
