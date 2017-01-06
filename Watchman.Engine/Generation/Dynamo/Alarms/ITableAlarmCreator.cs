using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace Watchman.Engine.Generation.Dynamo.Alarms
{
    public interface ITableAlarmCreator
    {
        Task EnsureReadCapacityAlarm(TableDescription table, string alarmNameSuffix,
            double thresholdFraction,
            string snsTopicArn, bool dryRun);

        Task EnsureReadThrottleAlarm(TableDescription table, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun);

        Task EnsureWriteCapacityAlarm(TableDescription table, string alarmNameSuffix,
            double thresholdFraction,
            string snsTopicArn, bool dryRun);

        Task EnsureWriteThrottleAlarm(TableDescription table, string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun);

        int AlarmPutCount { get; }
    }
}
