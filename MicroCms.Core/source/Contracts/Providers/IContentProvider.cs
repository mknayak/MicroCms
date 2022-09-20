using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Contracts.Providers
{
    /// <summary>
    /// IContentProvider
    /// </summary>
    public interface IContentProvider
    {
        /// <summary>
        /// Finds the by identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public T? FindById<T>(string id);
        /// <summary>
        /// Finds the template by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Template? FindTemplateById(string id);
        /// <summary>
        /// Finds the item by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Item? FindItemById(string id);
        /// <summary>
        /// Adds the template.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="fields">The fields.</param>
        public void AddTemplate(string templateName,string parentId,params TemplateField[] fields);
    }
}
