using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Contracts.Repositories
{
    public interface IContentRepository
    {
        /// <summary>
        /// Finds the template by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Template FindTemplateById(string id);
        /// <summary>
        /// Finds the item by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Item FindItemById(string id);
        /// <summary>
        /// Adds the template.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        public string AddTemplate(string templateName, string parentId, params TemplateField[] fields);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="templateId"></param>
        /// <param name="parentId"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public string AddTemplate(string templateName, string templateId, string parentId, params TemplateField[] fields);
        /// <summary>
        /// Updates the template.
        /// </summary>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="fields">The fields.</param>
        public void UpdateTemplate(string templateId, params TemplateField[] fields);
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        public string AddItem(string name, string templateId, string parentId, IDictionary<string, string> fields);
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        public string AddItem(string name,string itemId, string templateId, string parentId, IDictionary<string, string> fields);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public IEnumerable<Item> ChildItems(string itemId);
        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="fields">The fields.</param>
        public void UpdateItem(string itemId, string templateId, IDictionary<string, object> fields);
        /// <summary>
        /// Initialize the repository
        /// </summary>
        public void Initialize();
    }
}
