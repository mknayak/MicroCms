namespace MicroCms.Package
{
    public class CmsPackage
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public List<PackageItem> Data { get; set; }
        public List<PackageItem> Content { get; set; }
        public List<PackageTemplate> Template { get; set; }
    }

}