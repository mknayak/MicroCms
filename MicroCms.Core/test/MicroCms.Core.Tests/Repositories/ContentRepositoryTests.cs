using MicroCms.Core.Models.Templates;
using MicroCms.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MicroCms.Core.Tests
{
    public class ContentRepositoryTests
    {
        [Fact]
        public void ContentRepository_Constructor_Test()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            Assert.NotNull(repository);
        }
        [Fact]
        public void AddItem_ValidParameters_PersistData()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            string name = "TestItem";
            string template = "TestTemplate";
            string parentId = Guid.NewGuid().ToString();
            TemplateFieldGroup fieldGroup = new TemplateFieldGroup() { Name = "Default" };
            fieldGroup.Fields = new List<TemplateField>() {
                    new TemplateField(){Name="Field1", Type= TemplateFieldType.Text}
            };
            var templateId = repository.AddTemplate(template, parentId, fieldGroup);
            var itemId = repository.AddItem(name, templateId, parentId, new Dictionary<string, object> { { "Field1", "FieldValue" } });

            var item = repository.FindItemById(itemId);

            Assert.NotNull(repository);
            Assert.NotNull(item);
            Assert.NotNull(item.Fields);
            Assert.NotEmpty(item.Fields);
            Assert.Equal("Field1", item.Fields[0].Name);
            Assert.Equal("FieldValue", item.Fields[0].Value);
        }
        [Fact]
        public void AddItem_InValidTemplateId_ThrowsException()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            string name = "TestItem";
            string templateId = Guid.NewGuid().ToString();
            string parentId = Guid.NewGuid().ToString();

            Assert.Throws<ArgumentNullException>(() => repository.AddItem(name, templateId, parentId, new Dictionary<string, object> { { "Field1", "FieldValue" } }));
        }
        [Fact]
        public void AddTemplate_ValidParameters_PersistData()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            string template = "TestTemplate";
            string parentId = Guid.NewGuid().ToString();
            TemplateFieldGroup fieldGroup = new TemplateFieldGroup() { Name = "Default" };
            fieldGroup.Fields = new List<TemplateField>() {
                    new TemplateField(){Name="Field1", Type= TemplateFieldType.Text}
            };
            var templateId = repository.AddTemplate(template, parentId, fieldGroup);

            var item = repository.FindTemplateById(templateId);

            Assert.NotNull(repository);
            Assert.NotNull(item);
            Assert.NotNull(item.FieldGroups);
            Assert.Equal("Default", item.FieldGroups.ElementAt(0).Name);
        }

        [Fact]
        public void FindItem_InValidItemId_ReturnsNull()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            string itemId = Guid.NewGuid().ToString();
            var item = repository.FindItemById(itemId);

            Assert.NotNull(repository);
            Assert.Null(item);
        }
        [Fact]
        public void FindTemplate_InValidId_ReturnsNull()
        {
            ContentRepository repository = new ContentRepository(new Models.ExecutionContext());
            string templateId = Guid.NewGuid().ToString();
            var item = repository.FindTemplateById(templateId);

            Assert.NotNull(repository);
            Assert.Null(item);
        }

    }
}