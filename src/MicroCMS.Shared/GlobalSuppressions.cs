using System.Diagnostics.CodeAnalysis;

// CA1716: Namespace 'MicroCMS.Shared.*' uses reserved keyword 'Shared'
// Justification: 'Shared' is an established project naming convention in this codebase and
// the risk of conflict with VB.NET consumers is acceptable given this is a .NET 8 C#-first project.
[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Established project naming convention; .NET 8 C#-first project",
    Scope = "namespace",
    Target = "~N:MicroCMS.Shared")]

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Established project naming convention; .NET 8 C#-first project",
    Scope = "namespace",
    Target = "~N:MicroCMS.Shared.Results")]

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Established project naming convention; .NET 8 C#-first project",
    Scope = "namespace",
    Target = "~N:MicroCMS.Shared.Primitives")]

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Established project naming convention; .NET 8 C#-first project",
    Scope = "namespace",
    Target = "~N:MicroCMS.Shared.Ids")]

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Established project naming convention; .NET 8 C#-first project",
    Scope = "namespace",
    Target = "~N:MicroCMS.Shared.Guards")]
