namespace MicroCms.Core.Contracts.Providers
{
    public interface ICacheProvider
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value);
        T? GetOrSet<T>(string key, Func<string, T> valFunction);
        T? Remove<T>(string key);
        void Clear();
    }
}
