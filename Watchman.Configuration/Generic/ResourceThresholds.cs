using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public class ResourceThresholds
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public Dictionary<string, AlarmValues> Values { get; set; }
        public Dictionary<string, AlarmValues> Thresholds
        {
            get { return Values; }
            set { Values = value; }
        }

        public static implicit operator ResourceThresholds(string text)
        {
            return new ResourceThresholds
            {
                Name = text
            };
        }

        public override string ToString()
        {
            return Name ?? Pattern;
        }
    }
}
