using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Sqs
{
    public interface IQueueAlarmCreator
    {
        Task EnsureLengthAlarm(string queueName, int threshold,
            string alarmNameSuffix,
            string snsTopicArn, bool dryRun);

        Task EnsureOldestMessageAlarm(string queueName, int threshold,
            string alarmNameSuffix,
            string snsTopicArn, bool dryRun);

        int AlarmPutCount { get; }
    }
}
