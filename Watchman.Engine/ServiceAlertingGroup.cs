using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    public class ServiceAlertingGroup<TServiceConfig> : IServiceAlertingGroup
        where TServiceConfig : class
    {
        public AlertingGroupParameters GroupParameters { get; set; }

        public AwsServiceAlarms<TServiceConfig> Service { get; set; }

        IAwsServiceAlarms IServiceAlertingGroup.Service => this.Service;
    }
}
