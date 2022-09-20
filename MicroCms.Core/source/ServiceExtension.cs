using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCms.Core
{
    public static class ServiceExtension
    {
        public static void AddMicroCms(this IServiceCollection serviceProvider, IConfiguration configuration)
        {
            var option = MicroCmsConfigurationOption.Default;
            serviceProvider.AddSingleton(typeof(IContentProvider), Type.GetType(option.ContentProvider));
            serviceProvider.AddSingleton(typeof(ICacheProvider), Type.GetType(option.CacheProvider));
            serviceProvider.AddSingleton(typeof(IContentRepository), Type.GetType(option.ContentRepository));
            serviceProvider.AddSingleton(typeof(Models.ExecutionContext), new Models.ExecutionContext());
        }
        
    }
    public class MicroCmsConfigurationOption
    {
        public bool CacheEnabled { get; set; }
        public string ContentProvider { get; set; }
        public string CacheProvider { get; set; }
        public string ContentRepository { get; set; }


        public static MicroCmsConfigurationOption Default { get; set; }
        static MicroCmsConfigurationOption()
        {
            Default = new MicroCmsConfigurationOption() { 
                CacheEnabled = true ,
                ContentProvider = "MicroCms.Core.Providers.Content.DefaultContentProvider,MicroCms.Core",
                CacheProvider = "MicroCms.Core.Providers.Cache.DefaultCacheProvider,MicroCms.Core",
                ContentRepository = "MicroCms.Core.Repositories.ContentRepository,MicroCms.Core"
            };
        }

    }
}