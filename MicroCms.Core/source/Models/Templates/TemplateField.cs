namespace MicroCms.Core.Models.Templates
{
    public class TemplateField : BaseModel
    {
        public Guid TemplateGroupId { get; set; }
        public TemplateFieldType Type { get; set; }
    }
    public enum TemplateFieldType
    {
        Text,
        Number,
        Date,
        Image,
        Xml
    }
}
