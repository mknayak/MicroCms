namespace MicroCms.Core.Models
{
    public abstract class BaseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

}
