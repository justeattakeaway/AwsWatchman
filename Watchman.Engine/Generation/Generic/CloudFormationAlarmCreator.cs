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
        private readonly List<Tuple<ServiceAlertingGroup, IList<Alarm>>> _alarms = new List<Tuple<ServiceAlertingGroup, IList<Alarm>>>();

        public CloudFormationAlarmCreator(
            ICloudformationStackDeployer stack,
            IAlarmLogger logger)
        {
            _stack = stack;
            _logger = logger;
        }
        
        public void AddAlarms(ServiceAlertingGroup group, IList<Alarm> alarms)
        {

            foreach (var alarm in alarms)
            {

                if (alarm.AlarmDefinition.Threshold.ThresholdType != ThresholdType.Absolute)
                {
                    throw new Exception("Threshold type must be absolute for creation");
                }
            }

            _alarms.Add(Tuple.Create(group, alarms));
        }

        public async Task SaveChanges(bool dryRun)
        {
            var failedStacks = 0;

            foreach (var group in _alarms)
            {
                var alarms = group.Item2;
                var alertingGroup = group.Item1;

                var suffix = alertingGroup.Name.ToLowerInvariant();

                var stackName = "Watchman-" + suffix;
                try
                {
                    await GenerateAndDeployStack(
                        alarms,
                        alertingGroup.Targets,
                        alertingGroup.Name,
                        stackName,
                        dryRun);
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

        private async Task GenerateAndDeployStack(IEnumerable<Alarm> alarms, IEnumerable<AlertTarget> targets,
            string groupName,
            string stackName, bool dryRun)
        {
            alarms = alarms.ToList();
            if (!alarms.Any())
            {
                // todo, we should actually continue here but will change in later PR as want to keep behaviour same during refactor
                _logger.Info($"{stackName} would have no alarms, skipping");
                return;
            }

            var template = new CloudWatchCloudFormationTemplate(groupName, targets.ToList());
            template.AddAlarms(alarms);
            var json = template.WriteJson();

            await _stack.DeployStack(stackName, json, dryRun);
        }
    }
}
