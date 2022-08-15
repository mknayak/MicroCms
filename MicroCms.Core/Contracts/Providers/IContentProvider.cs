using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Contracts.Providers
{
    public interface IContentProvider
    {
        public T FindById<T>(Guid id);

        public Template FindTemplateById(Guid id);
        public Item FindItemById(Guid id);
    }
}
