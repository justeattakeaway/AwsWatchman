using System.Threading.Tasks;
using Watchman.Configuration;

namespace Watchman.Engine.Generation
{
    public interface IServiceAlarmTasks
    {
        Task GenerateAlarmsForService(WatchmanConfiguration config, RunMode mode);
    }

    public interface IServiceAlarmTasks<T, TAlarmConfig> : IServiceAlarmTasks
        where T : class
        where TAlarmConfig : class
    {

    }
}
