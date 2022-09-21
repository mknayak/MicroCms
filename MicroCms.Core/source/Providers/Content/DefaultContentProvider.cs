using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Providers.Content
{
    internal class DefaultContentProvider : IContentProvider
    {
        /// <summary>
        /// The content repository
        /// </summary>
        private readonly IContentRepository contentRepository;
        /// <summary>
        /// The cache provider
        /// </summary>
        private readonly ICacheProvider cacheProvider;
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultContentProvider"/> class.
        /// </summary>
        /// <param name="contentRepository">The content repository.</param>
        /// <param name="cacheProvider">The cache provider.</param>
        public DefaultContentProvider(IContentRepository contentRepository, ICacheProvider cacheProvider)
        {
            this.contentRepository = contentRepository;
            this.cacheProvider = cacheProvider;
        }
        /// <summary>
        /// Adds the template.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="fields">The fields.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AddTemplate(string templateName, string parentId, params TemplateField[] fields)
        {
            contentRepository.AddTemplate(templateName, parentId, fields);  
        }

        public IEnumerable<Item> ChildItems(string itemId)
        {
            return contentRepository.ChildItems(itemId);
        }

        /// <summary>
        /// Finds the by identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public T? FindById<T>(string id)
        {
            string key = $"{nameof(FindById)}-{id}";
            var item = FindItemById(id);
            var fieldDict= item?.Fields.ToDictionary(c=>c.Name, c=>c.Value);
            var obj = System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(fieldDict));
            return obj;
        }
        /// <summary>
        /// Finds the item by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Item? FindItemById(string id)
        {
            string key = $"{nameof(FindItemById)}-{id}";
            return cacheProvider.GetOrSet(key, (key) => contentRepository.FindItemById(id.ToString()));
        }
        /// <summary>
        /// Finds the template by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Template? FindTemplateById(string id)
        {
            string key = $"{nameof(FindTemplateById)}-{id}";
            return cacheProvider.GetOrSet(key, (key) => contentRepository.FindTemplateById(id.ToString()));
        }
    }
}
