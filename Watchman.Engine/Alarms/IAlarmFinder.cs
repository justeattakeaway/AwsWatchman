using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;

namespace Watchman.Engine.Alarms
{
    public interface IAlarmFinder
    {
        Task<MetricAlarm> FindAlarmByName(string alarmName);
        Task<IReadOnlyCollection<MetricAlarm>> AllAlarms();
        int Count { get; }
    }
}
