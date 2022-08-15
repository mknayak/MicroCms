namespace MicroCms.Core.Providers.Cache
{
    public class DefaultCacheProvider : ICacheProvider
    {
        static Dictionary<string, object> _cache = new Dictionary<string, object>();
        public void Clear()
        {
            _cache.Clear();
        }

        public T? Get<T>(string key)
        {
            return _cache.ContainsKey(key) ? (T)_cache[key] : default;
        }

        public T? GetOrSet<T>(string key, Func<string, T> valFunction)
        {
            var value = Get<T>(key);
            if (null == value)
                Set<T>(key, valFunction(key));
            return Get<T>(key);
        }

        public T? Remove<T>(string key)
        {
            var value = Get<T>(key);
            if (null != value)
                _cache.Remove(key);
            return value;
        }

        public void Set<T>(string key, T value)
        {
            if (null == value)
                return;
            if (_cache.ContainsKey(key))
                _cache[key] = value;
            else
                _cache.Add(key, value);
        }
    }
}
