using System.Collections.Generic;

namespace Watchman.Engine
{
    public class WatchmanServiceConfiguration
    {
        public string ServiceName { get;  }
        public IList<ServiceAlertingGroup> AlertingGroups { get; }
        public IList<AlarmDefinition> Defaults { get; }

        public WatchmanServiceConfiguration(string serviceName, IList<ServiceAlertingGroup> alertingGroups, IList<AlarmDefinition> defaults)
        {
            ServiceName = serviceName;
            AlertingGroups = alertingGroups;
            Defaults = defaults;
        }
    }
}
