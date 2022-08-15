using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Repositories
{
    public class CacheableContentRepository : IContentRepository
    {
        private readonly ICacheProvider cacheProvider;

        public CacheableContentRepository(ICacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
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
