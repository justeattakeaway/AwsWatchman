using System.Collections.Generic;

namespace Watchman.Engine.LegacyTracking
{
    public interface ILegacyAlarmTracker
    {
        void Register(string name);
        IReadOnlyCollection<string> ActiveAlarmNames { get; }
    }
}
