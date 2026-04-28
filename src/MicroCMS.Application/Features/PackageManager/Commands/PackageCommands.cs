using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.PackageManager.Dtos;

namespace MicroCMS.Application.Features.PackageManager.Commands;

/// <summary>Exports a full site package as a ZIP byte stream.</summary>
[HasPolicy(ContentPolicies.PackageExport)]
public sealed record ExportPackageCommand(ExportOptions Options) : ICommand<ExportPackageResult>;

/// <summary>The result of a successful export.</summary>
public sealed record ExportPackageResult(byte[] ZipBytes, string FileName);

/// <summary>Analyses an uploaded package archive and returns stats without applying changes.</summary>
[HasPolicy(ContentPolicies.PackageImport)]
public sealed record AnalysePackageCommand(
    byte[] ZipBytes,
    Guid TargetTenantId,
    Guid TargetSiteId) : ICommand<PackageAnalysisResult>;

/// <summary>Applies a previously-uploaded package to the target site.</summary>
[HasPolicy(ContentPolicies.PackageImport)]
public sealed record ImportPackageCommand(
    byte[] ZipBytes,
    Guid TargetTenantId,
    Guid TargetSiteId,
    ImportOptions Options) : ICommand<ImportProgress>;
