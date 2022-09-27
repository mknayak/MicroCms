namespace MicroCms.Package
{
    public class PackageItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TemplateId { get; set; }
        public Dictionary<string,string> Fields { get; set; }
        public IEnumerable<PackageItem> ChildItems { get; set; }
    }
    public class PackageItemField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}