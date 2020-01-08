using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Watchman.Tests.Fakes
{
    static class TemplateExtensions
    {
        public static List<Dimension> Dimensions(this Resource r)
        {
            var arr = (JArray) r
                .Properties["Dimensions"];

            var yyy = arr.ToObject<List<Dimension>>();

            return yyy;
        }

        public static string Dimension(this Resource r, string dimension)
        {
            return r.Dimensions()
                .SingleOrDefault(d => d.Name == dimension)
                ?.Value;
        }

        public static Dictionary<string, List<Resource>> AlarmsByDimension(this Template t, string dimension)
        {
            return t
                .Resources
                .Where(kvp => kvp.Value.Type == "AWS::CloudWatch::Alarm")
                .Select(z => new
                {
                    dimension = z.Value.Dimension(dimension),
                    resource = z.Value
                })
                .Where(z => z.dimension != null)
                .GroupBy(
                    z => z.dimension,
                    z => z.resource,
                    (table, alarms) => new { table, alarms })
                .ToDictionary(z => z.table, z => z.alarms.ToList());
        }

        public static Dictionary<string, List<Resource>> AlarmsByNamespace(this Template t)
        {
            return t
                .Resources
                .Where(kvp => kvp.Value.Type == "AWS::CloudWatch::Alarm")
                .Select(x => new
                {
                    ns = x.Value.Properties["Namespace"].Value<string>(),
                    alarm = x.Value
                })
                .GroupBy(x => x.ns, x => x.alarm)
                .ToDictionary(x => x.Key, grp => grp.ToList());
        }

        public static IList<Resource> Alarms(this Template t)
        {
            return t
                .Resources
                .Where(kvp => kvp.Value.Type == "AWS::CloudWatch::Alarm")
                .Select(x => x.Value)
                .ToList();
        }
    }
}
