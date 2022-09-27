using MicroCms.Core;
using MicroCms.Core.Contracts.Repositories;
using MicroCms.Core.Extensions;
using MicroCms.Core.Models.Items;
using MicroCms.Core.Models.Templates;
using Newtonsoft.Json;

namespace MicroCms.Package
{
    public interface ICmsPacakgeRepository
    {
        public void Install(string pathToPackageFile);
    }
    public class CmsPackageRepository : ICmsPacakgeRepository
    {
        private readonly IContentRepository contentRepository;

        public CmsPackageRepository(IContentRepository contentRepository)
        {
            this.contentRepository = contentRepository;
        }
        public void Install(string pathToPackageFile)
        {
            var fileContent = File.ReadAllText(pathToPackageFile);
            JsonSerializerSettings serializerOptions = new JsonSerializerSettings
            {
                MaxDepth = 100,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                NullValueHandling = NullValueHandling.Ignore,
            };
            var cmsPackage = JsonConvert.DeserializeObject<CmsPackage>(fileContent, serializerOptions);
            if (cmsPackage == null)
                throw new Exception("Invalid package file");
            //1. Install Templates
            if (cmsPackage.Template != null)
            {
                //1. Create a folder with package name
                var packageTemplateRootFolderId = contentRepository.AddItem(cmsPackage.Name, Constants.Ids.FolderTemplateId, Constants.Ids.TemplateRootId, null);
                foreach (var item in cmsPackage.Template)
                {
                    var templateFields = item.Fields.Select(c => new TemplateField(c.Name)
                    {
                        Type = (TemplateFieldType)Enum.Parse(typeof(TemplateFieldType), c.Type),
                        Group = c.Group,
                        Id = c.Id.ToId()
                    });
                    contentRepository.AddTemplate(item.Name,item.Id, packageTemplateRootFolderId, templateFields.ToArray());
                }
            }
            //2. Install Data
            if (cmsPackage.Data != null)
            {
                //1. Create a folder with package name
                var packageDataRootFolderId = contentRepository.AddItem(cmsPackage.Name, Constants.Ids.FolderTemplateId, Constants.Ids.DataRootId, null);
                foreach (var item in cmsPackage.Data)
                {
                    AddItem(packageDataRootFolderId, item);
                }
            }
            //3. Install Content
            if (cmsPackage.Content != null)
            {
                //1. Create a folder with package name
                var packageContentRootFolderId = contentRepository.AddItem(cmsPackage.Name, Constants.Ids.FolderTemplateId, Constants.Ids.ContenRootId, null);
                foreach (var item in cmsPackage.Content)
                {
                    AddItem(packageContentRootFolderId, item);
                }
            }
            //4. Store Package details
        }

        private void AddItem(string parentId, PackageItem item)
        {
            if (string.IsNullOrEmpty(item.TemplateId))
                item.TemplateId = Constants.Ids.FolderTemplateId;
            var itemId = contentRepository.AddItem(item.Name, item.TemplateId, parentId, item.Fields);
            if (null != item.ChildItems)
            {
                foreach (var subItem in item.ChildItems)
                {
                    AddItem(itemId, subItem);
                }
            }
        }
    }

}