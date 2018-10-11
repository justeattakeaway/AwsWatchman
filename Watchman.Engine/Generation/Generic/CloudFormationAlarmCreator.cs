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
        private readonly Dictionary<AlertingGroupParameters, List<Alarm>> _alarms = new Dictionary<AlertingGroupParameters, List<Alarm>>();

        public CloudFormationAlarmCreator(
            ICloudformationStackDeployer stack,
            IAlarmLogger logger)
        {
            _stack = stack;
            _logger = logger;
        }

        public void AddAlarms(AlertingGroupParameters group, IList<Alarm> alarms)
        {
            foreach (var alarm in alarms)
            {
                if (alarm.AlarmDefinition.Threshold.ThresholdType != ThresholdType.Absolute)
                {
                    throw new Exception("Threshold type must be absolute for creation");
                }
            }

            if (_alarms.ContainsKey(group))
            {
                _alarms[group].AddRange(alarms);
            }
            else
            {
                _alarms.Add(group, alarms.ToList());
            }
        }

        private void CheckForDuplicateStackNames()
        {
            var hasDuplicates = _alarms
                .Keys
                .Select(StackName)
                .GroupBy(_ => _)
                .Any(g => g.Count() > 1);

            if (hasDuplicates)
            {
                throw new Exception("Cannot deploy: multiple stacks would be created with the same name");
            }
        }

        private string StackName(AlertingGroupParameters group)
        {
            return "Watchman-" + group.Name.ToLowerInvariant();
        }

        public async Task SaveChanges(bool dryRun)
        {
            var failedStacks = 0;

            CheckForDuplicateStackNames();

            foreach (var group in _alarms)
            {
                var alarms = group.Value;
                var alertingGroup = group.Key;

                var stackName = StackName(alertingGroup);

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

        private async Task GenerateAndDeployStack(
            IEnumerable<Alarm> alarms,
            IEnumerable<AlertTarget> targets,
            string groupName,
            string stackName,
            bool dryRun)
        {
            alarms = alarms.Where(a => a.AlarmDefinition.Enabled).ToList();

            var onlyUpdateExisting = !alarms.Any();

            var template = new CloudWatchCloudFormationMultiStackTemplate(2, groupName, targets.ToList());
            template.AddAlarms(alarms);
            var jsons = template.WriteJson();

            for (int i = 0; i< jsons.Count() ; i++)
            {
                var groupStackName = i > 0 ? $"{stackName}-{i}" : stackName;
                await _stack.DeployStack(groupStackName, jsons[i], dryRun, onlyUpdateExisting);
            }
        }
    }
}
