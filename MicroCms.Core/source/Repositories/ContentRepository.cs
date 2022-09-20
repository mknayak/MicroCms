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
        public string AddItem(string name, string templateId, string parentId, IDictionary<string, object> fields)
        {
            var template = _dbStore.templates.ContainsKey(templateId) ? _dbStore.templates[templateId] : null;
            if (null == template)
                throw new ArgumentNullException(nameof(template));
            Item item = new Item
            {
                Name = name,
                ParentId =parentId,
                Enabled = true,
                TemplateId = template.Id
            };
            _dbStore.items.Add(item.Id.ToString(), item);

            foreach (var field in fields)
            {
                var fiedlId = Guid.NewGuid().ToString();
                _dbStore.itemFields.Add(fiedlId.ToString(), new ItemField { Id = fiedlId, ItemId = item.Id, Name = field.Key, Value = Convert.ToString(field.Value) });
            }

            return item.Id.ToString();
        }

        public string AddTemplate(string templateName, string parentId, params TemplateField[] fields)
        {
            Template template = new Template
            {
                Name = templateName,
                ParentId = parentId
            };
            _dbStore.templates.Add(template.Id.ToString(), template);
            foreach (var field in fields)
            {
                field.TemplateId = template.Id;
                _dbStore.templateFields.Add(field.Id.ToString(), field);
            }
            return template.Id.ToString();
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
                        cfield = new ItemField
                        { Id = Guid.NewGuid().ToString(), Name = field.Key };
                        _dbStore.itemFields.Add(cfield.Id.ToString(), cfield);
                    }
                    cfield.Value = Convert.ToString(field.Value);
                    cfield.UpdatedDate = DateTime.Now;
                }
            }
        }

        public void UpdateTemplate(string templateId, params TemplateField[] fields)
        {
            throw new NotImplementedException();
        }

        public class DbStore
        {
            public Dictionary<string, Template> templates = new Dictionary<string, Template>();
            public Dictionary<string, TemplateField> templateFields = new Dictionary<string, TemplateField>();
            //public Dictionary<string, TemplateFieldGroup> templateFieldGroups = new Dictionary<string, TemplateFieldGroup>();
            public Dictionary<string, Item> items = new Dictionary<string, Item>();
            public Dictionary<string, ItemField> itemFields = new Dictionary<string, ItemField>();
            public Dictionary<string, string> itemHierarchy = new Dictionary<string, string>();
        }
    }
}
