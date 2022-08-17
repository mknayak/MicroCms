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
        public T? FindById<T>(Guid id);
        /// <summary>
        /// Finds the template by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Template? FindTemplateById(Guid id);
        /// <summary>
        /// Finds the item by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Item? FindItemById(Guid id);
    }
}
