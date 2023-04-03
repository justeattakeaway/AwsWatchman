using Amazon.CloudWatch.Model;
using Watchman.Engine.Alarms;
using Watchman.Engine.Logging;

namespace Watchman.Engine.LegacyTracking
{
    public interface IOrphanedAlarmReporter
    {
        Task<IReadOnlyList<MetricAlarm>> FindOrphanedAlarms();
    }

    public class OrphanedAlarmReporter : IOrphanedAlarmReporter
    {
        private readonly ILegacyAlarmTracker _tracker;
        private readonly IAlarmFinder _finder;

        public OrphanedAlarmReporter(ILegacyAlarmTracker tracker, IAlarmFinder finder, IAlarmLogger logger)
        {
            _tracker = tracker;
            _finder = finder;
        }

        public async Task<IReadOnlyList<MetricAlarm>> FindOrphanedAlarms()
        {
            var allAlarmsBeforeRun = await _finder.AllAlarms();
            var tracked = _tracker
                .ActiveAlarmNames;

            var relevant = allAlarmsBeforeRun
                .Where(a => a.AlarmDescription != null)
                // all alarms we own should have this, unless they are really really old

                .Where(a =>  a.AlarmDescription.IndexOf("Watchman",
                                 StringComparison.InvariantCultureIgnoreCase) >= 0)

                // newer CloudFormation alarms have this in the name
                // we don't care about them here
                .Where(a =>  a.AlarmDescription.IndexOf("Alerting group",
                                 StringComparison.InvariantCultureIgnoreCase) < 0)
                .ToList();

            var unmatched = relevant
                .GroupJoin(tracked, alarm => alarm.AlarmName, name => name,
                    (alarm, enumerable) => (alarm: alarm, owned: enumerable.Any()))
                .Where(x => !x.owned)
                .Select(x => x.alarm)
                .ToArray();


            return unmatched;
        }
    }
}
