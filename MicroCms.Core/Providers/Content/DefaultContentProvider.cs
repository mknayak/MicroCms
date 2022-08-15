using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Providers.Content
{
    internal class DefaultContentProvider : IContentProvider
    {
        private readonly IContentRepository contentRepository;
        private readonly ICacheProvider cacheProvider;

        public DefaultContentProvider(IContentRepository contentRepository, ICacheProvider cacheProvider)
        {
            this.contentRepository = contentRepository;
            this.cacheProvider = cacheProvider;
        }
        public T FindById<T>(Guid id)
        {
            throw new NotImplementedException();
        }

        public Item FindItemById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Template FindTemplateById(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
