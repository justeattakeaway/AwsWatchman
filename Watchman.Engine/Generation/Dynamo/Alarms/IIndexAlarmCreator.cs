using Amazon.DynamoDBv2.Model;

namespace Watchman.Engine.Generation.Dynamo.Alarms
{
    public interface IIndexAlarmCreator
    {
        Task EnsureReadCapacityAlarm(TableDescription table, GlobalSecondaryIndexDescription index,
            string alarmNameSuffix,
            double thresholdFraction,
            string snsTopicArn, bool dryRun);

        Task EnsureReadThrottleAlarm(TableDescription table, GlobalSecondaryIndexDescription index,
            string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun);

        Task EnsureWriteCapacityAlarm(TableDescription table,
            GlobalSecondaryIndexDescription index, string alarmNameSuffix,
            double thresholdFraction,
            string snsTopicArn, bool dryRun);

        Task EnsureWriteThrottleAlarm(TableDescription table, GlobalSecondaryIndexDescription index,
            string alarmNameSuffix,
            double threshold,
            string snsTopicArn, bool dryRun);

        int AlarmPutCount { get; }
    }
}
