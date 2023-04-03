namespace Watchman.Engine.Generation.Generic
{
    public interface IAlarmCreator
    {
        void AddAlarms(AlertingGroupParameters group, IList<Alarm> alarms);
        Task SaveChanges(bool dryRun);
    }
}
