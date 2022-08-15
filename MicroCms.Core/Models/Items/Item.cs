namespace MicroCms.Core.Models.Items
{
    public class Item : BaseModel
    {
        public Item()
        {
            Fields = new List<ItemField>();
        }
        public Guid TemplateId { get; set; } 
        public string Path { get; set; }
        public List<ItemField> Fields { get; set; }

    }
}
