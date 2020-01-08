using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Watchman.Configuration.Generic
{
    public sealed class ResourceThresholds<TConfig> : IResource
        where TConfig : class
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        
        public string Description { get; set; }
        public Dictionary<string, AlarmValues> Values { get; set; }

        public TConfig Options { get; set; }

        public override string ToString()
        {
            return Name ?? Pattern;
        }

        public ResourceThresholds<TConfig> AsNamed(string name)
        {
            return new ResourceThresholds<TConfig>
            {
                Name = name,
                Pattern = null,
                Values = Values,
                Options = Options,
                Description = Description
            };
        }
        
        public ResourceThresholds<TConfig> AsPattern()
        {
            var name = Regex.Escape(Name);

            return new ResourceThresholds<TConfig>()
            {
                Pattern = $"^{name}$",
                Values = Values,
                Options = Options,
                Description = Description
            };
        }
    }
}
