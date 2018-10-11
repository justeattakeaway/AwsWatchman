using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Generic
{
    public class CloudWatchCloudFormationMultiStackTemplate
    {
        private readonly int _numberOfStacks;

        private readonly List<CloudWatchCloudFormationTemplate> _cloudWatchTemplates;


        public CloudWatchCloudFormationMultiStackTemplate(int numberOfStacks, string groupName, IList<AlertTarget> targets)
        {
            _numberOfStacks = numberOfStacks;
            _cloudWatchTemplates = new List<CloudWatchCloudFormationTemplate>();

            for (int i = 0; i < numberOfStacks; i++)
            {
                var innerGroupName = i > 0 ? $"{groupName}-i" : groupName;
                _cloudWatchTemplates.Add(new CloudWatchCloudFormationTemplate(innerGroupName, targets));
            }
        }

        public void AddAlarms(IEnumerable<Alarm> alarms)
        {
            foreach (var alarm in alarms)
            {
                _cloudWatchTemplates[Bucket(alarm.AlarmName, _numberOfStacks)].AddAlarm(alarm);
            }
        }

        public List<string> WriteJson()
        {
            return _cloudWatchTemplates.Select(c => c.WriteJson()).ToList();
        }

        int Bucket(string input, int numberOfBuckets)
        {
            var h = input.Select(c => (int)c).Sum();
            return h % numberOfBuckets;
        }

    }
}
