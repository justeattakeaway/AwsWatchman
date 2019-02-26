using System;

namespace Watchman.Configuration.Generic
{
    public class DynamoResourceConfig : IServiceAlarmConfig<DynamoResourceConfig>
    {
        public static bool MonitorWritesDefault = true;

        public static bool ThresholdIsAbsoluteDefault = false;

        public bool? MonitorWrites { get; set; }

        public bool? ThresholdIsAbsolute { get; set; }

        public DynamoResourceConfig Merge(DynamoResourceConfig parentConfig)
        {
            if (parentConfig == null)
            {
                throw new ArgumentNullException(nameof(parentConfig));
            }

            return new DynamoResourceConfig()
            {
                MonitorWrites = MonitorWrites ?? parentConfig.MonitorWrites,
                ThresholdIsAbsolute = ThresholdIsAbsolute ?? parentConfig.ThresholdIsAbsolute
            };
        }
    }
}
