using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Watchman.Configuration;

namespace Watchman.Engine
{
    public class AlertingGroupParameters
    {
        public string Name { get; }
        public string Description { get; }
        public string AlarmNameSuffix { get; }

        public int NumberOfCloudFormationStacks { get; }

        public IReadOnlyCollection<AlertTarget> Targets { get; }

        public bool IsCatchAll { get; }

        public AlertingGroupParameters(
            string name,
            string alarmNameSuffix,
            List<AlertTarget> targets = null,
            bool isCatchAll = false,
            string description = null,
            int numberOfCloudFormationStacks = 1)
        {
            Name = name;
            AlarmNameSuffix = alarmNameSuffix;
            Targets = (targets ?? new List<AlertTarget>()).AsReadOnly();
            IsCatchAll = isCatchAll;
            Description = description;
            NumberOfCloudFormationStacks = numberOfCloudFormationStacks;
        }
        
        // from https://stackoverflow.com/a/263416/22224
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 486187739;
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + (Description ?? "").GetHashCode();
                hash = hash * 23 + AlarmNameSuffix.GetHashCode();

                foreach (var target in Targets)
                {
                    hash = hash * 23 + target.GetHashCode();
                }

                hash = hash * 23 + IsCatchAll.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            var compare = obj as AlertingGroupParameters;

            if (compare == null) return false;

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return compare.IsCatchAll == IsCatchAll
                && compare.Name == Name
                && compare.AlarmNameSuffix == AlarmNameSuffix
                && compare.Targets.SequenceEqual(Targets);
        }
    }
}
