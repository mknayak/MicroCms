using MicroCms.Core.Contracts.Providers;

namespace MicroCms.Core.Providers.Cache
{
    public class DefaultCacheProvider : ICacheProvider
    {
        /// <summary>
        /// The cache
        /// </summary>
        static Dictionary<string, object> _cache = new Dictionary<string, object>();
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T? Get<T>(string key)
        {
            return _cache.ContainsKey(key) ? (T)_cache[key] : default;
        }
        /// <summary>
        /// Gets the or set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="valFunction">The value function.</param>
        /// <returns></returns>
        public T? GetOrSet<T>(string key, Func<string, T> valFunction)
        {
            var value = Get<T>(key);
            if (null == value)
                Set<T>(key, valFunction(key));
            return Get<T>(key);
        }
        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T? Remove<T>(string key)
        {
            var value = Get<T>(key);
            if (null != value)
                _cache.Remove(key);
            return value;
        }
        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
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
