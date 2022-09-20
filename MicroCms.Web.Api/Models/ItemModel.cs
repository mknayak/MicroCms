using MicroCms.Core.Models.Templates;

namespace MicroCms.Web.Api.Models
{
    public class BaseModel
    {
        public string Id { get; set; }
        public string Name { get; set; }

    }
    public class ItemModel:BaseModel
    {
        public IEnumerable<ItemFieldModel> Fields { get; set; }
    }
    public class TemplateModel:BaseModel
    {
        public IEnumerable<TemplateFieldModel> Fields { get; set; }

    }
    public class ItemFieldModel : BaseModel
    {
        public string Value { get; set; }
    }
    public class TemplateFieldModel : BaseModel
    {
        public TemplateFieldType Type { get; set; }
    }
}
