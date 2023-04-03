namespace Watchman.Engine.Generation
{
    static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out TValue result))
            {
                return result;
            }

            return default(TValue);
        }
    }
}
