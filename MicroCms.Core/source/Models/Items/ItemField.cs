namespace MicroCms.Core.Models.Items
{
    public class ItemField: BaseEntity
    {
        public ItemField(string name):base(name)
        {

        }
        public string ItemId { get; set; }
        public string TemplateFieldId { get; set; }
        public string Value { get; set; }
        public string FullQualifiedName { get; set; }

    }
}
