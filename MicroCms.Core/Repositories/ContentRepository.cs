using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Repositories
{
    public class ContentRepository : IContentRepository
    {
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
