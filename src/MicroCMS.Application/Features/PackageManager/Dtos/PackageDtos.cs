namespace MicroCMS.Application.Features.PackageManager.Dtos;

// ─── Package manifest ─────────────────────────────────────────────────────────

/// <summary>Top-level manifest written into package.json within the ZIP archive.</summary>
public sealed record PackageManifest(
    string PackageVersion,
    string CreatedAt,
    Guid TenantId,
    Guid SiteId,
    string TenantSlug,
    string SiteName,
    PackageContents Contents);

public sealed record PackageContents(
    int ContentTypeCount,
    int EntryCount,
    int PageCount,
 int LayoutCount,
    int MediaMetadataCount,
    int ComponentCount,
    int UserCount,
    int SiteCount);

// ─── Export ───────────────────────────────────────────────────────────────────

/// <summary>Request options for the export command.</summary>
public sealed record ExportOptions(
    Guid TenantId,
    Guid SiteId,
    bool IncludeContentTypes = true,
    bool IncludeEntries = true,
    bool IncludePages = true,
    bool IncludeLayouts = true,
    bool IncludeMediaMetadata = true,
    bool IncludeComponents = true,
  bool IncludeUsers = false,
    bool IncludeSiteSettings = true);

// ─── Import analysis ──────────────────────────────────────────────────────────

/// <summary>Result returned after analyzing an uploaded package (before applying it).</summary>
public sealed record PackageAnalysisResult(
PackageManifest Manifest,
    IReadOnlyList<PackageItemStat> Items,
    IReadOnlyList<string> Warnings);

public sealed record PackageItemStat(
    string Category,
    int TotalInPackage,
    int NewItems,
    int ExistingItems);

// ─── Import options ───────────────────────────────────────────────────────────

/// <summary>What to import and how to handle conflicts.</summary>
public sealed record ImportOptions(
    bool ImportContentTypes = true,
    bool ImportEntries = true,
    bool ImportPages = true,
    bool ImportLayouts = true,
    bool ImportMediaMetadata = true,
    bool ImportComponents = true,
    bool ImportUsers = false,
    bool ImportSiteSettings = true,
    ConflictResolution ConflictResolution = ConflictResolution.Skip);

public enum ConflictResolution
{
    Skip = 0,
    Overwrite = 1,
}

// ─── Import result ────────────────────────────────────────────────────────────

/// <summary>Progressive status sent during import; also the final summary.</summary>
public sealed record ImportProgress(
    string Status,               // "Running" | "Completed" | "Failed"
    string CurrentStep,
    int TotalSteps,
    int CompletedSteps,
    IReadOnlyList<ImportStepResult> StepResults,
    string? ErrorMessage = null);

public sealed record ImportStepResult(
    string Category,
    int Imported,
    int Skipped,
    int Overwritten,
    int Failed,
    IReadOnlyList<string> Errors);
