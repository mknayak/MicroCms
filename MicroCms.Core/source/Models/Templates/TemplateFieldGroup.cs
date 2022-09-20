namespace MicroCms.Core.Models.Templates
{
    public class TemplateFieldGroup : BaseEntity
    {
        public TemplateFieldGroup()
        {
            Fields= Enumerable.Empty<TemplateField>();
        }
        public Guid TemplateId { get; set; }

        public IEnumerable<TemplateField> Fields { get; set; }

    }

}
