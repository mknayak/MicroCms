using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCms.Core.Providers.Cache
{
    internal interface ICacheProvider
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value);
        T? GetOrSet<T>(string key,Func<string,T> valFunction);
        T? Remove<T>(string key);
        void Clear();
    }
}
