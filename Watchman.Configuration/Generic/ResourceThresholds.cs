using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public interface IResource
    {
        string Name { get; }
        string Pattern { get; }
        Dictionary<string, AlarmValues> Values { get; set; }
    }

    public class ResourceThresholds<TConfig> : IResource
        where TConfig : class
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public Dictionary<string, AlarmValues> Values { get; set; }

        public TConfig Parameters { get; set; }

        // todo: replace with json parser
        /*public static implicit operator ResourceThresholds(string text)
        {
            return new ResourceThresholds
            {
                Name = text
            };
        }*/

        public override string ToString()
        {
            return Name ?? Pattern;
        }
    }
}
