using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCms.Core
{
    public static class ServiceExtension
    {
        public static MicroCmsConfiguration AddMicroCms(this IServiceCollection serviceProvider, MicroCmsConfigurationOption option)
        {
            option = option ?? MicroCmsConfigurationOption.Default;
            Type? contentProviderType = Type.GetType(option.ContentProvider);
            if (contentProviderType != null)
                serviceProvider.AddSingleton(typeof(IContentProvider), contentProviderType);
            Type? cacheProviderType = Type.GetType(option.CacheProvider);
            if (cacheProviderType != null)
                serviceProvider.AddSingleton(typeof(ICacheProvider), cacheProviderType);
            Type? contentRepositoryType = Type.GetType(option.ContentRepository);
            if (contentRepositoryType != null)
                serviceProvider.AddSingleton(typeof(IContentRepository), contentRepositoryType);
            serviceProvider.AddSingleton(typeof(Models.ExecutionContext), new Models.ExecutionContext());

            return new MicroCmsConfiguration { Options=option, ServiceProvider=serviceProvider};
        }
        public static void InitializeMicroCms(this IApplicationBuilder app)
        {            
           var contentRepository= app.ApplicationServices.GetService<IContentRepository>();
            if (null != contentRepository)
                contentRepository.Initialize();
        }

    }
    public class MicroCmsConfigurationOption
    {
        public const string ConfigSectionKey = "MicroCms";
        private const string DefaultContentProvider = "MicroCms.Core.Providers.Content.DefaultContentProvider,MicroCms.Core";
        private const string DefaultCacheProvider = "MicroCms.Core.Providers.Cache.DefaultCacheProvider,MicroCms.Core";
        private const string DefaultContentRepository = "MicroCms.Core.Repositories.ContentRepository,MicroCms.Core";

        public bool CacheEnabled { get; set; } = false;
        public string ContentProvider { get; set; } = DefaultContentProvider;
        public string CacheProvider { get; set; } = DefaultCacheProvider;
        public string ContentRepository { get; set; } = DefaultContentRepository;


        public static MicroCmsConfigurationOption Default { get; set; }
        static MicroCmsConfigurationOption()
        {
            Default = new MicroCmsConfigurationOption()
            {
                CacheEnabled = true
            };
        }

    }
    public class MicroCmsConfiguration
    {
        public IServiceCollection ServiceProvider { get; set; }
        public MicroCmsConfigurationOption Options { get; set; }
    }
}