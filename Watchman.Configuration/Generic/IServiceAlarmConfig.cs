using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.Configuration.Generic
{
    public interface IServiceAlarmConfig<TConfig>
    {
        TConfig Merge(TConfig parentConfig);
    }
}
