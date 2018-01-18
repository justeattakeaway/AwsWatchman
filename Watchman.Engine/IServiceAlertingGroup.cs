using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    public interface IServiceAlertingGroup
    {
        AlertingGroupParameters GroupParameters { get; }

        IAwsServiceAlarms Service { get; }
    }
}
