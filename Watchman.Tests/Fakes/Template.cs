using Newtonsoft.Json.Linq;

namespace Watchman.Tests.Fakes
{
    public class Template
    {
        public Dictionary<string, Resource> Resources { get; set; }
    }

    public class Dimension
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Resource
    {
        public string Type { get; set; }
        public Dictionary<string, JToken> Properties { get; set; }
    }
}
