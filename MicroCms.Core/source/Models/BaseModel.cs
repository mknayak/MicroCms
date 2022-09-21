namespace MicroCms.Core.Models
{
    public abstract class BaseEntity
    {
        public BaseEntity(string name, string id)
        {
            Id = id;
            CreatedBy = Constants.DefaultCreatedBy;
            UpdatedBy = Constants.DefaultCreatedBy;
            CreatedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
            Name = name;
            Enabled = true;
        }
        public BaseEntity(string name) : this(name, Guid.NewGuid().ToString())
        {
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
