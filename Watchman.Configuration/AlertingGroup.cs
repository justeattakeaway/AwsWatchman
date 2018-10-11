using System.Collections.Generic;

namespace Watchman.Configuration
{
    public class AlertingGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string AlarmNameSuffix { get; set; }

        public List<AlertTarget> Targets { get; set; }

        public List<ReportTarget> ReportTargets { get; set; }

        public bool IsCatchAll { get; set; }

        public DynamoDb DynamoDb { get; set; }
        public Sqs Sqs { get; set; }

        public int NumberOfCloudFormationStacks { get; set; } = 1;

        public AlertingGroupServices Services { get; set; }

        public AlertingGroup()
        {
            Targets = new List<AlertTarget>();
            ReportTargets = new List<ReportTarget>();

            DynamoDb = new DynamoDb();
            Sqs = new Sqs();
        }
    }
}
