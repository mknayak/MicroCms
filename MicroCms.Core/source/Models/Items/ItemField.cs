namespace MicroCms.Core.Models.Items
{
    public class ItemField: BaseModel
    {
        public Guid ItemId { get; set; }
        public Guid TemplateFieldId { get; set; }
        public string Value { get; set; }
        public string FullQualifiedName { get; set; }

    }
}
