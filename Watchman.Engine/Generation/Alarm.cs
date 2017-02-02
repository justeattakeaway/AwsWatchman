using System.Collections.Generic;
using Amazon.CloudWatch.Model;
using Watchman.AwsResources;

namespace Watchman.Engine.Generation
{
    public class Alarm
    {
        public string AlarmName { get; set;  }
        public ServiceAlertingGroup AlertingGroup { get; set; }
        public List<Dimension> Dimensions { get; set; }
        public AlarmDefinition AlarmDefinition { get; set; }
        public IAwsResource Resource { get; set; }
        public string SnsTopicArn { get; set; }
    }
}