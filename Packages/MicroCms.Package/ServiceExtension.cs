using MicroCms.Core;
using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Validation;
using MicroCms.Package;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MicroCms.Pacakge
{
    public static class ServiceExtension
    {
        public static MicroCmsConfiguration AddPackageFeature(this MicroCmsConfiguration configuration)
        {
            var serviceProvider= configuration.ServiceProvider;
            serviceProvider.AddSingleton(typeof(ICmsPacakgeRepository), typeof(CmsPackageRepository));
            return configuration;
        }
        public static void AddSamplePackages(this IApplicationBuilder app)
        {            
           var contentRepository= app.ApplicationServices.GetService<ICmsPacakgeRepository>();
            string path = @"E:\Personal\MicroCms\Packages\MicroCms.Package\Samples\microcms-package-blog.json";
            if (null != contentRepository)
                contentRepository.Install(path);
        }

    }
}