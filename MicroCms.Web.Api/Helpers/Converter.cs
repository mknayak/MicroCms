using MicroCms.Core.Models;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;
using MicroCms.Core.Validation;
using MicroCms.Web.Api.Models;

namespace MicroCms.Web.Api.Helpers
{
    public static class Converter
    {
        public static ItemModel ToItemModel(this Item item)
        {
            Validate.Field(item, nameof(item)).IsNotNull();
            var model = new ItemModel()
            {
                Id = item.Id.ToString(),
                Name = item.Name,
                Fields = item.Fields.Select(x => ToItemFieldModel(x)).ToList()
            };
            return model;
        }
        public static ItemFieldModel ToItemFieldModel(this ItemField field)
        {
            Validate.Field(field, nameof(field)).IsNotNull();
            var fieldModel = new ItemFieldModel()
            {
                Id = field.Id.ToString(),
                Name = field.Name,
                Value = field.Value
            };
            return fieldModel;
        }
        public static TemplateModel ToTemplateModel(this Template template)
        {
            Validate.Field(template, nameof(template)).IsNotNull();
            var templateModel = new TemplateModel()
            {
                Id = template.Id.ToString(),
                Name = template.Name,
                Fields = template.Fields.Select(x => ToTemplateFieldModel(x)).ToList()
            };
            return templateModel;

        }
        public static TemplateFieldModel ToTemplateFieldModel(this TemplateField templateField)
        {
            Validate.Field(templateField, nameof(templateField)).IsNotNull();
            var templateFieldModel = new TemplateFieldModel()
            {
                Id = templateField.Id.ToString(),
                Name = templateField.Name,
                Type = templateField.Type
            };
            return templateFieldModel;
        }
        public static TemplateField ToTemplateField(this TemplateFieldModel templateFieldModel)
        {
            Validate.Field(templateFieldModel, nameof(templateFieldModel)).IsNotNull();
            var templateField = new TemplateField()
            {
                Id = templateFieldModel.Id.ToString(),
                Name = templateFieldModel.Name,
                Type = templateFieldModel.Type
            };
            return templateField;
        }

        
    }
}
