namespace MicroCms.Core.Models.Items
{
    public class Item : BaseEntity
    {
        public Item()
        {
            Fields = new List<ItemField>();
        }
        public string TemplateId { get; set; }
        public string Path { get; set; }
        public List<ItemField> Fields { get; set; }

    }
}
