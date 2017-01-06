using System.Collections.Generic;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    public class ServiceAlertingGroup
    {
        public string Name { get; set; }

        public string AlarmNameSuffix { get; set; }

        public List<AlertTarget> Targets { get; set; }

        public List<ReportTarget> ReportTargets { get; set; }

        public bool IsCatchAll { get; set; }

        public AwsServiceAlarms Service { get; set; }
    }
}
