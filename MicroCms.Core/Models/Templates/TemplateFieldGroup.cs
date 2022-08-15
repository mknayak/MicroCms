namespace MicroCms.Core.Models.Templates
{
    public class TemplateFieldGroup : BaseModel
    {
        public TemplateFieldGroup()
        {
            Fields= Enumerable.Empty<TemplateField>();
        }
        public IEnumerable<TemplateField> Fields { get; set; }

    }

}
