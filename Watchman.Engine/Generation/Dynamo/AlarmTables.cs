using System.Collections.Generic;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Dynamo
{
    public class AlarmTables
    {
        public string AlarmNameSuffix { get; set; }
        public string SnsTopicArn { get; set; }

        public double Threshold { get; set; }
        public bool MonitorThrottling { get; set; }

        public double ThrottlingThreshold { get; set; }

        public bool DryRun { get; set; }

        public List<Table> Tables { get; set; }
    }
}
