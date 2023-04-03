using Amazon.CloudWatch.Model;

namespace Watchman.Engine.Generation
{
    public class Alarm
    {
        public string AlarmName { get; set;  }
        public string AlarmDescription { get; set; }
        public List<Dimension> Dimensions { get; set; }
        public AlarmDefinition AlarmDefinition { get; set; }
        public string ResourceIdentifier { get; set; }
    }
}
