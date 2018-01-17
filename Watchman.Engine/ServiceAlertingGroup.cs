using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    public class ServiceAlertingGroup
    {
        public AlertingGroupParameters GroupParameters { get; set; }

        public AwsServiceAlarms Service { get; set; }
    }
}
