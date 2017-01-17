using System.Linq;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Dynamo
{
    public static class AlarmTablesHelper
    {
        public static AlarmTables FilterForRead(AlertingGroup alertingGroup)
        {
            var filteredTables = alertingGroup.DynamoDb.Tables
                .ExcludePrefixes(alertingGroup.DynamoDb.ExcludeTablesPrefixedWith, t => t.Name)
                .ExcludePrefixes(alertingGroup.DynamoDb.ExcludeReadsForTablesPrefixedWith, t => t.Name);

            return new AlarmTables
            {
                AlarmNameSuffix = alertingGroup.AlarmNameSuffix,
                Threshold = alertingGroup.DynamoDb.Threshold ?? AwsConstants.DefaultCapacityThreshold,
                MonitorThrottling = alertingGroup.DynamoDb.MonitorThrottling ?? true,
                Tables = filteredTables.ToList()
            };
        }

        public static AlarmTables FilterForWrite(AlertingGroup alertingGroup)
        {
            var filteredTables = alertingGroup.DynamoDb.Tables
                .ExcludePrefixes(alertingGroup.DynamoDb.ExcludeTablesPrefixedWith, t => t.Name)
                .ExcludePrefixes(alertingGroup.DynamoDb.ExcludeWritesForTablesPrefixedWith, t => t.Name);

            return new AlarmTables
            {
                AlarmNameSuffix = alertingGroup.AlarmNameSuffix,
                Threshold = alertingGroup.DynamoDb.Threshold ?? AwsConstants.DefaultCapacityThreshold,
                MonitorThrottling = alertingGroup.DynamoDb.MonitorThrottling ?? true,
                Tables = filteredTables.ToList()
            };
        }
    }
}
