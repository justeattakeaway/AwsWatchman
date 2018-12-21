using Newtonsoft.Json.Linq;
using Watchman.Tests.Fakes;

namespace Watchman.Tests
{
    static class ResourceExtensions
    {
        public static string GetPropertyValue(this Resource resource, string key)
        {
            return resource.Properties.ContainsKey(key)
                ? resource.Properties[key].Value<string>()
                : string.Empty;
        }
    }
}
