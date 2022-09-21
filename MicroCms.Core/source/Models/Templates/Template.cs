namespace MicroCms.Core.Models.Templates
{
    public class Template : BaseEntity
    {
        public Template(string name):base(name)
        {
            Fields = Enumerable.Empty<TemplateField>();
        }
        public IEnumerable<TemplateField> Fields { get; set; }
    }

}
