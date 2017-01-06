using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Generic
{
    public interface IAlarmCreator
    {
        void AddAlarm(Alarm alarm);
        Task SaveChanges(bool dryRun);
    }
}
