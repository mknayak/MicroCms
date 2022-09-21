namespace MicroCms.Core.Models.Items
{
    public class Item : BaseEntity
    {
        public Item(string name):base(name)
        {
            Fields = new List<ItemField>();
        }
        public string TemplateId { get; set; }
        public string Path { get; set; }
        public List<ItemField> Fields { get; set; }

    }
}
