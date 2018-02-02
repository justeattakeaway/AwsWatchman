using System.Threading.Tasks;
using Watchman.Configuration;

namespace Watchman.Engine.Generation
{
    public interface IServiceAlarmTasks
    {
        Task GenerateAlarmsForService(WatchmanConfiguration config, RunMode mode);
    }
}
