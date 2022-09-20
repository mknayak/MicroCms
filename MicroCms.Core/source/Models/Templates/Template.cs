namespace MicroCms.Core.Models.Templates
{
    public class Template : BaseEntity
    {
        public Template()
        {
            Fields = Enumerable.Empty<TemplateField>();
        }
        public IEnumerable<TemplateField> Fields { get; set; }
    }

}
