namespace Watchman.Engine.Generation
{
    public static class Filters
    {
        /// <summary>
        /// Filter the source to exclude all items where the selector starts with any of the prefixes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="prefixes"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExcludePrefixes<T>(this IEnumerable<T> source,
            IEnumerable<string> prefixes, Func<T, string> selector)
        {
            return source.Where(item => !prefixes.Any(pre => selector(item).StartsWith(pre)));
        }
    }
}
