using System;

namespace Watchman.Configuration.Generic
{
    public class AutoScalingResourceConfig : IServiceAlarmConfig<AutoScalingResourceConfig>
    {
        public int? ScaleUpDelay { get; set; }
        public AutoScalingResourceConfig Merge(AutoScalingResourceConfig parentConfig)
        {
            if (parentConfig == null)
            {
                throw new ArgumentNullException(nameof(parentConfig));
            }

            return new AutoScalingResourceConfig()
            {
                ScaleUpDelay = ScaleUpDelay ?? parentConfig.ScaleUpDelay
            };
        }
    }
}
