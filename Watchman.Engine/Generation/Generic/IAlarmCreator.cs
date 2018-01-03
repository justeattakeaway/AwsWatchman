using System.Collections.Generic;
using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Generic
{
    public interface IAlarmCreator
    {
        void AddAlarms(ServiceAlertingGroup group, IList<Alarm> alarms);
        Task SaveChanges(bool dryRun);
    }
}
