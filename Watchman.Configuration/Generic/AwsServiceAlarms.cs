using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public class AwsServiceAlarms
    {
        public List<ResourceThresholds> Resources { get; set; }

        public List<string> ExcludeResourcesPrefixedWith { get; set; }

        public Dictionary<string, AlarmValues> Values { get; set; }

        public Dictionary<string, AlarmValues> Thresholds
        {
            get { return Values; }
            set { Values = value; }
        }
    }
}
