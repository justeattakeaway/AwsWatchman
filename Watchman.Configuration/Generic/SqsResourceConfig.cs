using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.Configuration.Generic
{
    public class SqsResourceConfig : IServiceAlarmConfig<SqsResourceConfig>
    {
        public bool? IncludeErrorQueues { get; set; }

        public SqsResourceConfig Merge(SqsResourceConfig parentConfig)
        {
            if (parentConfig == null)
            {
                throw new ArgumentNullException(nameof(parentConfig));
            }

            return new SqsResourceConfig()
                   {
                       IncludeErrorQueues = IncludeErrorQueues ?? parentConfig.IncludeErrorQueues
                   };
        }
    }

}
