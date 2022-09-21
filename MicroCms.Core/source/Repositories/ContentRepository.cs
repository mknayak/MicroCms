using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;

namespace MicroCms.Core.Repositories
{
    public class ContentRepository : IContentRepository
    {

        readonly DbStore _dbStore;

        public ContentRepository(Models.ExecutionContext context)
        {
            var key = $"{nameof(ContentRepository)}-{nameof(DbStore)}";
            if (context.Items.ContainsKey(key))
            {
                _dbStore = (DbStore)context.Items[key];
            }
            else

            {
                _dbStore = new DbStore();
                context.Items.Add(key, _dbStore);
            }
        }
        public string AddItem(string name, string templateId, string parentId, IDictionary<string, string> fields)
        {
            return AddItem(name, string.Empty, templateId, parentId, fields);
        }

        public string AddItem(string name, string itemId, string templateId, string parentId, IDictionary<string, string> fields)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = Guid.NewGuid().ToString();
            var template = _dbStore.templates.ContainsKey(templateId) ? _dbStore.templates[templateId] : null;
            if (null == template)
                throw new ArgumentNullException(nameof(template));
            Item item = new Item(name)
            {
                ParentId = parentId,
                TemplateId = template.Id,
                Id = itemId
            };
            _dbStore.items.Add(item.Id.ToString(), item);

            if (null != fields)
                foreach (var field in fields)
                {
                    var fiedlId = Guid.NewGuid().ToString();
                    _dbStore.itemFields.Add(fiedlId.ToString(), new ItemField(field.Key)
                    { Id = fiedlId, ItemId = item.Id, Value = field.Value });
                }

            return item.Id.ToString();
        }

        public string AddTemplate(string templateName, string parentId, params TemplateField[] fields)
        {
            return AddTemplate(templateName, string.Empty, parentId, fields);
        }

        public string AddTemplate(string templateName, string templateId, string parentId, params TemplateField[] fields)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                templateId = Guid.NewGuid().ToString();
            Template template = new Template(templateName)
            {
                ParentId = parentId,
                Id = templateId
            };
            _dbStore.templates.Add(template.Id.ToString(), template);
            foreach (var field in fields)
            {
                field.TemplateId = template.Id;
                _dbStore.templateFields.Add(field.Id.ToString(), field);
            }
            return template.Id.ToString();
        }

        public IEnumerable<Item> ChildItems(string itemId)
        {
            return _dbStore.items.Values.Where(c => c.ParentId == itemId);
        }

        public Item FindItemById(string id)
        {
            var item = _dbStore.items.ContainsKey(id) ? _dbStore.items[id] : null;
            if (null != item)
            {
                item.Fields = _dbStore.itemFields.Values.Where(c => c.ItemId == item.Id).ToList();
            }
            return item;
        }

        public Template FindTemplateById(string id)
        {
            var template = _dbStore.templates.ContainsKey(id) ? _dbStore.templates[id] : null;
            if (null != template)
            {
                template.Fields = _dbStore.templateFields.Values.Where(c => c.TemplateId == template.Id).ToList();
            }
            return template;
        }

        public void Initialize()
        {
            _dbStore.templates.Add(Constants.Ids.SystemFolderTemplateId, new Template("RootSystemNode")
            {
                Id = Constants.Ids.SystemFolderTemplateId
            });

            _dbStore.items.Add(Constants.Ids.ContenRootId, new Item("Content")
            {
                TemplateId = Constants.Ids.SystemFolderTemplateId,
                Id = Constants.Ids.ContenRootId
            });
            _dbStore.items.Add(Constants.Ids.DataRootId, new Item("Data")
            {
                TemplateId = Constants.Ids.SystemFolderTemplateId,
                Id = Constants.Ids.DataRootId
            });
            _dbStore.items.Add(Constants.Ids.TemplateRootId, new Item("Templates")
            {
                TemplateId = Constants.Ids.SystemFolderTemplateId,
                Id = Constants.Ids.TemplateRootId
            });
            _dbStore.items.Add(Constants.Ids.CoreTemplateFolderId, new Item("Core")
            {
                TemplateId = Constants.Ids.FolderTemplateId,
                Id = Constants.Ids.CoreTemplateFolderId,
                ParentId = Constants.Ids.TemplateRootId
            });
            _dbStore.templates.Add(Constants.Ids.FolderTemplateId, new Template("FolderTemplate")
            {
                Id = Constants.Ids.FolderTemplateId
            });
        }

        public void UpdateItem(string itemId, string templateId, IDictionary<string, object> fields)
        {
            var item = _dbStore.items.ContainsKey(itemId) ? _dbStore.items[itemId] : null;
            if (null != item)
            {
                var currentFields = _dbStore.itemFields.Values.Where(c => c.ItemId == item.Id).ToList();
                foreach (var field in currentFields.Where(c => !fields.ContainsKey(c.Name)))
                {
                    _dbStore.itemFields.Remove(field.Id.ToString());
                }
                foreach (var field in fields)
                {
                    var cfield = currentFields.FirstOrDefault(c => c.Name.ToString() == field.Key);
                    if (null == cfield)
                    {
                        cfield = new ItemField(field.Key);
                        _dbStore.itemFields.Add(cfield.Id.ToString(), cfield);
                    }
                    cfield.Value = Convert.ToString(field.Value);
                    cfield.UpdatedDate = DateTime.Now;
                }
            }
        }

        public void UpdateTemplate(string templateId, params TemplateField[] fields)
        {

        }

        public class DbStore
        {
            public Dictionary<string, Template> templates = new Dictionary<string, Template>();
            public Dictionary<string, TemplateField> templateFields = new Dictionary<string, TemplateField>();
            public Dictionary<string, Item> items = new Dictionary<string, Item>();
            public Dictionary<string, ItemField> itemFields = new Dictionary<string, ItemField>();
            public Dictionary<string, string> itemHierarchy = new Dictionary<string, string>();
        }
    }
}
