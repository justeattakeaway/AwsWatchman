namespace Watchman.Engine.LegacyTracking
{
    public class LegacyAlarmTracker : ILegacyAlarmTracker
    {
        private readonly HashSet<string> _alarmNames = new HashSet<string>();

        public IReadOnlyCollection<string> ActiveAlarmNames => _alarmNames;

        public void Register(string name)
        {
            _alarmNames.Add(name);
        }
    }
}
