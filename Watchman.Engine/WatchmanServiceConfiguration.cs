using System.Collections.Generic;

namespace Watchman.Engine
{
    public class WatchmanServiceConfiguration<TConfigType> where TConfigType:class
    {
        public string ServiceName { get;  }
        public IList<ServiceAlertingGroup<TConfigType>> AlertingGroups { get; }

        public WatchmanServiceConfiguration(string serviceName, IList<ServiceAlertingGroup<TConfigType>> alertingGroups)
        {
            ServiceName = serviceName;
            AlertingGroups = alertingGroups;
        }
    }
}
