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

                var stacks = Enumerable.Range(0, alertingGroup.NumberOfCloudFormationStacks)
                    .Select(stackIndex => (stackIndex, alarms: alarms
                            .Where(a => Bucket(a.AlarmName, alertingGroup.NumberOfCloudFormationStacks) == stackIndex)
                            .ToArray()
                        ))
                    .ToArray();

                foreach (var stack in stacks)
                {
                    var numberedStackName = stack.stackIndex > 0 ? $"{stackName}-{stack.stackIndex}" : stackName;

                    ApplyAlarmSuffix(stack.stackIndex, stack.alarms);

                    try
                    {
                        await GenerateAndDeployStack(
                            stack.alarms,
                            alertingGroup.Targets,
                            alertingGroup.Name,
                            numberedStackName,
                            dryRun);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error deploying stack {numberedStackName}");
                        failedStacks++;
                    }
                }
            }

            if (failedStacks > 0)
            {
                throw new WatchmanException(failedStacks + " stacks failed to deploy");
            }
        }

        private static void ApplyAlarmSuffix(int bucket, IEnumerable<Alarm> alarmsInBucket)
        {
            if (bucket > 0)
            {
                foreach (var alarm in alarmsInBucket)
                {
                    alarm.AlarmName += $"-{bucket}";
                }
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

            // if there are no alarms we want to update existing stacks (in case everything has been removed)
            // but we don't want to create a new one which is empty
            var onlyUpdateExisting = !alarms.Any();

            var template = new CloudWatchCloudFormationTemplate(groupName, targets.ToList());
            template.AddAlarms(alarms);
            var json = template.WriteJson();
            await _stack.DeployStack(stackName, json, dryRun, onlyUpdateExisting);
        }

        private int Bucket(string input, int numberOfBuckets)
        {
            var h = input.Select(c => (int)c).Sum();
            return h % numberOfBuckets;
        }
    }
}
