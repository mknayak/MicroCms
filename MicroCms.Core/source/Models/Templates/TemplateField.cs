using System.Diagnostics.CodeAnalysis;

namespace MicroCms.Core.Models.Templates
{
    public class TemplateField : BaseEntity
    {
        public TemplateField(string name):base(name)
        {
            Group = "Default";
        }
        public string? TemplateId { get; set; }
        public string Group { get; set; }
        public TemplateFieldType Type { get; set; }
    }
    public enum TemplateFieldType
    {
        Text,
        Number,
        Date,
        Image,
        File,
        Xml
    }
}
