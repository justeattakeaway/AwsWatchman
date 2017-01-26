using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public class AwsServiceAlarms
    {
        public List<ResourceThresholds> Resources { get; set; }

        public List<string> ExcludeResourcesPrefixedWith { get; set; }

        public Dictionary<string, ThresholdValue> Values { get; set; }

        public Dictionary<string, ThresholdValue> Thresholds
        {
            get { return Values; }
            set { Values = value; }
        }
    }
}
