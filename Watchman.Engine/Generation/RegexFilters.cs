using System.Text.RegularExpressions;

namespace Watchman.Engine.Generation
{
    public static class RegexFilters
    {
        public static IEnumerable<string> WhereRegexIsMatch(this IEnumerable<string> source,
            string regexPattern)
        {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return source.Where(item => regex.IsMatch(item));
        }
    }
}
