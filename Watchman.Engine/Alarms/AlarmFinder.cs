using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Alarms
{
    public class AlarmFinder : IAlarmFinder
    {
        private readonly IAlarmLogger _logger;
        private readonly IAmazonCloudWatch _cloudWatchClient;
        private IDictionary<string, MetricAlarm> _alarmData;

        public AlarmFinder(
            IAlarmLogger logger,
            IAmazonCloudWatch cloudWatchClient)
        {
            _logger = logger;
            _cloudWatchClient = cloudWatchClient;
        }

        private async Task<IDictionary<string, MetricAlarm>> ReadAllAlarms()
        {
            var alarms = new List<MetricAlarm>();
            string lastAlarmName = null;

            do
            {
                var request = new DescribeAlarmsRequest
                {
                    NextToken = lastAlarmName
                };

                var alarmsResponse = await _cloudWatchClient.DescribeAlarmsAsync(request);

                if (alarmsResponse != null)
                {
                    alarms.AddRange(alarmsResponse.MetricAlarms);
                    lastAlarmName = alarmsResponse.NextToken;
                }
                else
                {
                    lastAlarmName = null;
                }
            }
            while (lastAlarmName != null);

            _logger.Info($"Preloaded all {alarms.Count} alarms");

            return alarms.ToDictionary(a => a.AlarmName);
        }

        private async Task Preload()
        {
            if (_alarmData == null)
            {
                _alarmData = await ReadAllAlarms();
            }
        }

        public async Task<MetricAlarm> FindAlarmByName(string alarmName)
        {
            await Preload();

            if (_alarmData.ContainsKey(alarmName))
            {
                return _alarmData[alarmName];
            }

            return null;
        }

        public int Count => _alarmData?.Count ?? 0;
    }
}
