using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Watchman.Tests.Fakes
{
    static class TemplateExtensions
    {
        public static Dictionary<string, Resource> Alarms(this Template t)
        {
            return t
                .Resources
                .Where(kvp => kvp.Value.Type == "AWS::CloudWatch::Alarm")
                .ToDictionary(k => k.Key, k => k.Value);
        }

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
                .Alarms()
                .Values
                .Select(z => new
                {
                    dimension = z.Dimension(dimension),
                    resource = z
                })
                .Where(z => z.dimension != null)
                .GroupBy(
                    z => z.dimension,
                    z => z.resource,
                    (table, alarms) => new { table, alarms })
                .ToDictionary(z => z.table, z => z.alarms.ToList());
        }
    }
}
