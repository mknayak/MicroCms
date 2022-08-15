using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Contracts.Repositories
{
    public interface IContentRepository
    {
        public Template FindTemplateById(Guid id);
        public Item FindItemById(Guid id);
    }
}
