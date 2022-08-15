using MicroCms.Core.Contracts.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCms.Core
{
    public static class ServiceExtension
    {
        public static void AddMicroCms(this IServiceCollection serviceProvider, IConfiguration configuration)
        {
            var option = MicroCmsConfigurationOption.Default;
            serviceProvider.AddSingleton(typeof(ICacheProvider), Type.GetType(option.CacheProvider));
        }
        
    }
    public class MicroCmsConfigurationOption
    {
        public bool CacheEnabled { get; set; }
        public string CacheProvider { get; set; }
        public string DbProvider { get; set; }


        public static MicroCmsConfigurationOption Default { get; set; }
        static MicroCmsConfigurationOption()
        {
            Default = new MicroCmsConfigurationOption() { 
                CacheEnabled = true ,
                CacheProvider = "MicroCms.Core.Providers.Cache.DefaultCacheProvider,MicroCms.Core"
            };
        }

    }
}