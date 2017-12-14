using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.Configuration;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Generic
{
    public class CloudFormationAlarmCreator : IAlarmCreator
    {
        private readonly ICloudformationStackDeployer _stack;
        private readonly IAlarmLogger _logger;
        private readonly List<Alarm> _alarms = new List<Alarm>();

        public CloudFormationAlarmCreator(
            ICloudformationStackDeployer stack,
            IAlarmLogger logger)
        {
            _stack = stack;
            _logger = logger;
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

            var failedStacks = 0;

            foreach (var group in groupedBySuffix)
            {
                var stackName = "Watchman-" + group.Name;
                var alarms = group.Alarms;
                try
                {
                    await GenerateAndDeployStack(dryRun, alarms, stackName);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error deploying stack {stackName}");
                    failedStacks++;
                }
            }

            if (failedStacks > 0)
            {
                throw new WatchmanException(failedStacks + " stacks failed to deploy");
            }
        }

        private async Task GenerateAndDeployStack(bool dryRun, IEnumerable<Alarm> alarms, string stackName)
        {
            var template = new CloudWatchCloudFormationTemplate();
            template.AddAlarms(alarms);
            var json = template.WriteJson();

            await _stack.DeployStack(stackName, json, dryRun);
        }
    }
}
