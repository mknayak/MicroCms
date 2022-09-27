namespace MicroCms.Package
{
    public class PackageTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<PackageTemplateField> Fields { get; set; }
    }
    public class PackageTemplateField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Group { get; set; } = "Default";
    }
}