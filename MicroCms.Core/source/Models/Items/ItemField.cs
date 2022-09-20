namespace MicroCms.Core.Models.Items
{
    public class ItemField: BaseEntity
    {
        public string ItemId { get; set; }
        public string TemplateFieldId { get; set; }
        public string Value { get; set; }
        public string FullQualifiedName { get; set; }

    }
}
