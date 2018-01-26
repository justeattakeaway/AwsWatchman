using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public interface IResource
    {
        string Name { get; }
        string Pattern { get; }
        Dictionary<string, AlarmValues> Values { get; set; }
    }

    public sealed class ResourceThresholds<TConfig> : IResource
        where TConfig : class
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public Dictionary<string, AlarmValues> Values { get; set; }

        public TConfig Options { get; set; }

        public override string ToString()
        {
            return Name ?? Pattern;
        }
    }
}
