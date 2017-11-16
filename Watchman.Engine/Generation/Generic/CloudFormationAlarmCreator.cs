using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Watchman.Configuration;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Generic
{
    public class CloudFormationAlarmCreator : IAlarmCreator
    {
        private readonly ICloudformationStackDeployer _stack;
        private readonly List<Alarm> _alarms = new List<Alarm>();

        public CloudFormationAlarmCreator(ICloudformationStackDeployer stack)
        {
            _stack = stack;
        }

        public void AddAlarm(Alarm alarm)
        {
            if (alarm.AlarmDefinition.Threshold.ThresholdType != ThresholdType.Absolute)
            {
                throw new Exception("Threshold type must be absolute for creation");
            }

            _alarms.Add(alarm);
        }

        public async Task SaveChanges(bool dryRun)
        {
            var groupedBySuffix = _alarms
                .GroupBy(x => x.AlertingGroup.Name,
                    x => x,
                    (g, x) => new
                    {
                        // this is because a lot of the group suffixes are lower(group name)
                        // and it reduces the impact of moving stack naming from suffix to name
                        Name = g.ToLowerInvariant(),
                        Alarms = x
                    });

            foreach (var group in groupedBySuffix)
            {
                var stackName = "Watchman-" + group.Name;
                var template = new CloudWatchCloudFormationTemplate();
                template.AddAlarms(group.Alarms);
                var json = template.WriteJson();

                await _stack.DeployStack(stackName, json, dryRun);
            }
        }
    }
}
