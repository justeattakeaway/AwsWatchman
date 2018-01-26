using System.Collections.Generic;
using System.Linq;

namespace Watchman.Configuration.Generic
{
    public class AwsServiceAlarms<TResourceConfig>
        : IAwsServiceAlarms
        where TResourceConfig: class 
    {
        public List<ResourceThresholds<TResourceConfig>> Resources { get; set; }

        public List<string> ExcludeResourcesPrefixedWith { get; set; }

        public Dictionary<string, AlarmValues> Values { get; set; }

        public TResourceConfig Options { get; set; }

        List<IResource> IAwsServiceAlarms.Resources => Resources?.Select(r => (IResource) r).ToList();
    }
}
