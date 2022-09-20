namespace MicroCms.Core.Models
{
    public abstract class BaseEntity
    {
        public BaseEntity()
        {
            Id= Guid.NewGuid().ToString();
            CreatedBy = Constants.DefaultCreatedBy;
            UpdatedBy = Constants.DefaultCreatedBy;
            CreatedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
            Name = string.Empty;
        }
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

}
