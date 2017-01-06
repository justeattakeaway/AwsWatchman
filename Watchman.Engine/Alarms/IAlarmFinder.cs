using System.Threading.Tasks;
using Amazon.CloudWatch.Model;

namespace Watchman.Engine.Alarms
{
    public interface IAlarmFinder
    {
        Task<MetricAlarm> FindAlarmByName(string alarmName);
        int Count { get; }
    }
}
