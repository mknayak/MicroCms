namespace MicroCms.Core.Models.Templates
{
    public class TemplateField : BaseModel
    {
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
