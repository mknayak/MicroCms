using System.IO.Compression;
using System.Text;
using System.Text.Json;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.PackageManager.Dtos;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using ContentTypeAlias = MicroCMS.Domain.Aggregates.Content.ContentType;

namespace MicroCMS.Infrastructure.PackageManager;

/// <summary>
/// ZIP-based package service. Exports/imports full-site snapshots as structured JSON inside a ZIP archive.
/// </summary>
public sealed class PackageService(
    IRepository<ContentTypeAlias, ContentTypeId> contentTypeRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<Page, PageId> pageRepo,
    IRepository<Layout, LayoutId> layoutRepo,
    IRepository<MediaAsset, MediaAssetId> mediaRepo,
    IRepository<Component, ComponentId> componentRepo,
    IRepository<User, UserId> userRepo,
    IRepository<Tenant, TenantId> tenantRepo,
    IUnitOfWork unitOfWork) : IPackageService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ── Export ────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportAsync(ExportOptions opts, CancellationToken ct = default)
    {
        var siteId = new SiteId(opts.SiteId);
        var (contentTypes, entries, pages, layouts, media, components, users) =
           await LoadExportDataAsync(opts, siteId, ct);

        var tenant = await tenantRepo.GetByIdAsync(new TenantId(opts.TenantId), ct);
    var site = tenant?.Sites.FirstOrDefault(s => s.Id == siteId);

        var manifest = new PackageManifest(
            PackageVersion: "1.0",
            CreatedAt: DateTimeOffset.UtcNow.ToString("O"),
          TenantId: opts.TenantId,
         SiteId: opts.SiteId,
            TenantSlug: site?.Handle.Value ?? opts.SiteId.ToString("N"),
            SiteName: site?.Name ?? "Unknown",
   Contents: new PackageContents(
             ContentTypeCount: contentTypes.Count,
       EntryCount: entries.Count,
      PageCount: pages.Count,
                LayoutCount: layouts.Count,
         MediaMetadataCount: media.Count,
    ComponentCount: components.Count,
   UserCount: users.Count,
     SiteCount: 1));

        return BuildZip(manifest, contentTypes, entries, pages, layouts, media, components, users);
    }

    private async Task<(
   IReadOnlyList<ContentTypeAlias> ContentTypes,
        IReadOnlyList<Entry> Entries,
        IReadOnlyList<Page> Pages,
   IReadOnlyList<Layout> Layouts,
  IReadOnlyList<MediaAsset> Media,
        IReadOnlyList<Component> Components,
  IReadOnlyList<User> Users)> LoadExportDataAsync(ExportOptions opts, SiteId siteId, CancellationToken ct)
    {
        var contentTypes = opts.IncludeContentTypes
            ? await contentTypeRepo.ListAsync(new ContentTypesBySiteSpec(siteId), ct)
   : (IReadOnlyList<ContentTypeAlias>)[];
        var entries = opts.IncludeEntries
              ? await entryRepo.ListAsync(new EntriesBySiteSpec(siteId, null), ct)
       : (IReadOnlyList<Entry>)[];
        var pages = opts.IncludePages
   ? await pageRepo.ListAsync(new PagesBySiteSpec(siteId), ct)
   : (IReadOnlyList<Page>)[];
        var layouts = opts.IncludeLayouts
  ? await layoutRepo.ListAsync(new LayoutsBySiteSpec(siteId), ct)
            : (IReadOnlyList<Layout>)[];
        var media = opts.IncludeMediaMetadata
   ? await mediaRepo.ListAsync(new MediaAssetsBySiteSpec(siteId), ct)
         : (IReadOnlyList<MediaAsset>)[];
        var components = opts.IncludeComponents
               ? await componentRepo.ListAsync(new AllComponentsBySiteSpec(siteId), ct)
           : (IReadOnlyList<Component>)[];
        var users = opts.IncludeUsers
                  ? await userRepo.ListAsync(new AllUsersPagedSpec(1, 10_000), ct)
        : (IReadOnlyList<User>)[];

        return (contentTypes, entries, pages, layouts, media, components, users);
    }

    // ── Analyse ───────────────────────────────────────────────────────────

    public async Task<PackageAnalysisResult> AnalyseAsync(
      byte[] zipBytes, Guid targetTenantId, Guid targetSiteId, CancellationToken ct = default)
    {
        var pkg = ReadPackage(zipBytes);
        var siteId = new SiteId(targetSiteId);
        var warnings = new List<string>();

        if (pkg.Manifest.SiteId != targetSiteId)
            warnings.Add($"Package was exported from site '{pkg.Manifest.SiteId}' — importing into '{targetSiteId}'.");

        var stats = new List<PackageItemStat>();

        if (pkg.ContentTypes.Count > 0)
        {
            var existing = await contentTypeRepo.ListAsync(new ContentTypesBySiteSpec(siteId), ct);
            var existingHandles = existing.Select(c => c.Handle).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("ContentTypes", pkg.ContentTypes.Count,
              pkg.ContentTypes.Count(c => !existingHandles.Contains(c.Handle)),
              pkg.ContentTypes.Count(c => existingHandles.Contains(c.Handle))));
        }

        if (pkg.Entries.Count > 0)
        {
            var existingEntries = await entryRepo.ListAsync(new EntriesBySiteSpec(siteId, null), ct);
            var existingKeys = existingEntries.Select(e => $"{e.Slug.Value}|{e.Locale.Value}").ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Entries", pkg.Entries.Count,
      pkg.Entries.Count(e => !existingKeys.Contains($"{e.Slug}|{e.Locale}")),
      pkg.Entries.Count(e => existingKeys.Contains($"{e.Slug}|{e.Locale}"))));
        }

        if (pkg.Pages.Count > 0)
        {
            var existingPages = await pageRepo.ListAsync(new PagesBySiteSpec(siteId), ct);
            var existingSlugs = existingPages.Select(p => p.Slug.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Pages", pkg.Pages.Count,
            pkg.Pages.Count(p => !existingSlugs.Contains(p.Slug)),
           pkg.Pages.Count(p => existingSlugs.Contains(p.Slug))));
        }

        if (pkg.Layouts.Count > 0)
        {
            var existingLayouts = await layoutRepo.ListAsync(new LayoutsBySiteSpec(siteId), ct);
            var existingKeys = existingLayouts.Select(l => l.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Layouts", pkg.Layouts.Count,
          pkg.Layouts.Count(l => !existingKeys.Contains(l.Key)),
  pkg.Layouts.Count(l => existingKeys.Contains(l.Key))));
        }

        if (pkg.Media.Count > 0)
        {
            var existingMedia = await mediaRepo.ListAsync(new MediaAssetsBySiteSpec(siteId), ct);
            var existingNames = existingMedia.Select(m => m.Metadata.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Media", pkg.Media.Count,
      pkg.Media.Count(m => !existingNames.Contains(m.FileName)),
          pkg.Media.Count(m => existingNames.Contains(m.FileName))));
        }

        if (pkg.Components.Count > 0)
        {
            var existingComponents = await componentRepo.ListAsync(new AllComponentsBySiteSpec(siteId), ct);
            var existingKeys = existingComponents.Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Components", pkg.Components.Count,
                pkg.Components.Count(c => !existingKeys.Contains(c.Key)),
     pkg.Components.Count(c => existingKeys.Contains(c.Key))));
        }

        if (pkg.Users.Count > 0)
        {
            var existingUsers = await userRepo.ListAsync(new AllUsersPagedSpec(1, 10_000), ct);
            var existingEmails = existingUsers.Select(u => u.Email.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            stats.Add(new PackageItemStat("Users", pkg.Users.Count,
            pkg.Users.Count(u => !existingEmails.Contains(u.Email)),
             pkg.Users.Count(u => existingEmails.Contains(u.Email))));
        }

        return new PackageAnalysisResult(pkg.Manifest, stats, warnings);
    }

    // ── Import ────────────────────────────────────────────────────────────

    public async Task<ImportProgress> ImportAsync(
   byte[] zipBytes, Guid targetTenantId, Guid targetSiteId,
  ImportOptions opts, CancellationToken ct = default)
    {
        var pkg = ReadPackage(zipBytes);
        var tenantId = new TenantId(targetTenantId);
        var siteId = new SiteId(targetSiteId);
        var stepResults = await RunImportStepsAsync(pkg, tenantId, siteId, opts, ct);

        var hasErrors = stepResults.Any(s => s.Failed > 0);
        return new ImportProgress(
                 Status: hasErrors ? "CompletedWithErrors" : "Completed",
         CurrentStep: "Done",
                 TotalSteps: stepResults.Count,
      CompletedSteps: stepResults.Count,
                 StepResults: stepResults);
    }

    private async Task<List<ImportStepResult>> RunImportStepsAsync(
        PackageData pkg, TenantId tenantId, SiteId siteId, ImportOptions opts, CancellationToken ct)
    {
        var stepResults = new List<ImportStepResult>();
        await AddSchemaStepsAsync(stepResults, pkg, tenantId, siteId, opts, ct);
        await AddContentStepsAsync(stepResults, pkg, tenantId, siteId, opts, ct);
        return stepResults;
    }

    private async Task AddSchemaStepsAsync(
       List<ImportStepResult> results, PackageData pkg, TenantId tenantId, SiteId siteId,
      ImportOptions opts, CancellationToken ct)
    {
        if (opts.ImportContentTypes && pkg.ContentTypes.Count > 0)
            results.Add(await ImportContentTypesAsync(pkg.ContentTypes, tenantId, siteId, opts.ConflictResolution, ct));
        if (opts.ImportComponents && pkg.Components.Count > 0)
            results.Add(await ImportComponentsAsync(pkg.Components, tenantId, siteId, opts.ConflictResolution, ct));
        if (opts.ImportLayouts && pkg.Layouts.Count > 0)
            results.Add(await ImportLayoutsAsync(pkg.Layouts, tenantId, siteId, opts.ConflictResolution, ct));
    }

    private async Task AddContentStepsAsync(
      List<ImportStepResult> results, PackageData pkg, TenantId tenantId, SiteId siteId,
   ImportOptions opts, CancellationToken ct)
    {
        if (opts.ImportEntries && pkg.Entries.Count > 0)
            results.Add(await ImportEntriesAsync(pkg.Entries, tenantId, siteId, opts.ConflictResolution, ct));
        if (opts.ImportPages && pkg.Pages.Count > 0)
            results.Add(await ImportPagesAsync(pkg.Pages, tenantId, siteId, opts.ConflictResolution, ct));
        if (opts.ImportMediaMetadata && pkg.Media.Count > 0)
            results.Add(await ImportMediaAsync(pkg.Media, tenantId, siteId, opts.ConflictResolution, ct));
        if (opts.ImportUsers && pkg.Users.Count > 0)
            results.Add(await ImportUsersAsync(pkg.Users, tenantId, opts.ConflictResolution, ct));
    }

    // ── ZIP builder ───────────────────────────────────────────────────────

    private static byte[] BuildZip(
        PackageManifest manifest,
     IEnumerable<ContentTypeAlias> contentTypes,
        IEnumerable<Entry> entries,
        IEnumerable<Page> pages,
        IEnumerable<Layout> layouts,
        IEnumerable<MediaAsset> media,
   IEnumerable<Component> components,
        IEnumerable<User> users)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(archive, "package.json", manifest);
            WriteZipEntry(archive, "content-types.json", contentTypes.Select(MapContentType).ToList());
            WriteZipEntry(archive, "entries.json", entries.Select(MapEntry).ToList());
            WriteZipEntry(archive, "pages.json", pages.Select(MapPage).ToList());
            WriteZipEntry(archive, "layouts.json", layouts.Select(MapLayout).ToList());
            WriteZipEntry(archive, "media.json", media.Select(MapMedia).ToList());
            WriteZipEntry(archive, "components.json", components.Select(MapComponent).ToList());
            WriteZipEntry(archive, "users.json", users.Select(MapUser).ToList());
        }
        return ms.ToArray();
    }

    private static void WriteZipEntry<T>(ZipArchive archive, string entryName, T data)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(JsonSerializer.Serialize(data, _json));
    }

    // ── ZIP reader ────────────────────────────────────────────────────────

    private static PackageData ReadPackage(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        return new PackageData(
    Manifest: ReadZipEntry<PackageManifest>(archive, "package.json")
      ?? throw new InvalidDataException("package.json missing or malformed."),
   ContentTypes: ReadZipEntry<List<ContentTypePackageData>>(archive, "content-types.json") ?? [],
    Entries: ReadZipEntry<List<EntryPackageData>>(archive, "entries.json") ?? [],
      Pages: ReadZipEntry<List<PagePackageData>>(archive, "pages.json") ?? [],
      Layouts: ReadZipEntry<List<LayoutPackageData>>(archive, "layouts.json") ?? [],
   Media: ReadZipEntry<List<MediaMetadataPackageData>>(archive, "media.json") ?? [],
      Components: ReadZipEntry<List<ComponentPackageData>>(archive, "components.json") ?? [],
          Users: ReadZipEntry<List<UserPackageData>>(archive, "users.json") ?? []);
    }

    private static T? ReadZipEntry<T>(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        if (entry is null) return default;
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), _json);
    }

    // ── Import steps ──────────────────────────────────────────────────────

    private async Task<ImportStepResult> ImportContentTypesAsync(
        IReadOnlyList<ContentTypePackageData> items, TenantId tenantId, SiteId siteId,
    ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await contentTypeRepo.ListAsync(new ContentTypesBySiteSpec(siteId), ct);
        var existingByHandle = existing.ToDictionary(c => c.Handle, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                if (existingByHandle.TryGetValue(data.Handle, out var existingCt))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    existingCt.Update(data.DisplayName, data.Description);
                    contentTypeRepo.Update(existingCt);
                    overwritten++;
                }
                else
                {
                    var newCt = ContentTypeAlias.Create(tenantId, siteId, data.Handle, data.DisplayName, data.Description);
                    AddFieldsToContentType(newCt, data.Fields);
                    await contentTypeRepo.AddAsync(newCt, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Handle}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("ContentTypes", imported, skipped, overwritten, failed, errors);
    }

    private async Task<ImportStepResult> ImportEntriesAsync(
      IReadOnlyList<EntryPackageData> items, TenantId tenantId, SiteId siteId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await entryRepo.ListAsync(new EntriesBySiteSpec(siteId, null), ct);
        var existingByKey = existing.ToDictionary(
            e => $"{e.Slug.Value}|{e.Locale.Value}", StringComparer.OrdinalIgnoreCase);
        var cts = await contentTypeRepo.ListAsync(new ContentTypesBySiteSpec(siteId), ct);
        var ctByHandle = cts.ToDictionary(c => c.Handle, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                var key = $"{data.Slug}|{data.Locale}";
                if (existingByKey.TryGetValue(key, out var existingEntry))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    if (!string.IsNullOrWhiteSpace(data.FieldsJson))
                        existingEntry.UpdateFields(data.FieldsJson, Guid.Empty, "Package import");
                    entryRepo.Update(existingEntry);
                    overwritten++;
                }
                else
                {
                    if (!ctByHandle.TryGetValue(data.ContentTypeHandle, out var contentType))
                    {
                        errors.Add($"Entry '{data.Slug}': ContentType '{data.ContentTypeHandle}' not found.");
                        failed++;
                        continue;
                    }
                    var slug = Slug.Create(data.Slug);
                    var locale = Locale.Create(data.Locale);
                    var entry = Entry.Create(tenantId, siteId, contentType.Id, slug, locale,
                 Guid.Empty, data.FieldsJson ?? "{}");
                    await entryRepo.AddAsync(entry, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Slug}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Entries", imported, skipped, overwritten, failed, errors);
    }

    private async Task<ImportStepResult> ImportPagesAsync(
        IReadOnlyList<PagePackageData> items, TenantId tenantId, SiteId siteId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await pageRepo.ListAsync(new PagesBySiteSpec(siteId), ct);
        var existingBySlug = existing.ToDictionary(p => p.Slug.Value, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                if (existingBySlug.TryGetValue(data.Slug, out var existingPage))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    existingPage.UpdateTitle(data.Title);
                    pageRepo.Update(existingPage);
                    overwritten++;
                }
                else
                {
                    var slug = Slug.Create(data.Slug);
                    var page = Page.CreateStatic(tenantId, siteId, data.Title, slug, depth: data.Depth);
                    await pageRepo.AddAsync(page, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Slug}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Pages", imported, skipped, overwritten, failed, errors);
    }

    private async Task<ImportStepResult> ImportLayoutsAsync(
        IReadOnlyList<LayoutPackageData> items, TenantId tenantId, SiteId siteId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await layoutRepo.ListAsync(new LayoutsBySiteSpec(siteId), ct);
        var existingByKey = existing.ToDictionary(l => l.Key, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                var templateType = Enum.TryParse<LayoutTemplateType>(data.TemplateType, out var lt)
           ? lt : LayoutTemplateType.Handlebars;

                if (existingByKey.TryGetValue(data.Key, out var existingLayout))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    existingLayout.Update(data.Name, templateType);
                    existingLayout.UpdateZones(data.ZonesJson);
                    existingLayout.UpdateDefaultPlacements(data.DefaultPlacementsJson);
                    layoutRepo.Update(existingLayout);
                    overwritten++;
                }
                else
                {
                    var layout = Layout.Create(tenantId, siteId, data.Name, data.Key, templateType);
                    layout.UpdateZones(data.ZonesJson);
                    layout.UpdateDefaultPlacements(data.DefaultPlacementsJson);
                    if (data.IsDefault) layout.MarkAsDefault();
                    await layoutRepo.AddAsync(layout, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Key}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Layouts", imported, skipped, overwritten, failed, errors);
    }

    private async Task<ImportStepResult> ImportMediaAsync(
        IReadOnlyList<MediaMetadataPackageData> items, TenantId tenantId, SiteId siteId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await mediaRepo.ListAsync(new MediaAssetsBySiteSpec(siteId), ct);
        var existingByName = existing.ToDictionary(m => m.Metadata.FileName, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                if (existingByName.TryGetValue(data.FileName, out var existingAsset))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    UpdateExistingMedia(existingAsset, data);
                    overwritten++;
                }
                else
                {
                    var asset = BuildMediaAsset(tenantId, siteId, data);
                    await mediaRepo.AddAsync(asset, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.FileName}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Media", imported, skipped, overwritten, failed, errors);
    }

    private void UpdateExistingMedia(MediaAsset asset, MediaMetadataPackageData data)
    {
        asset.UpdateAltText(data.AltText);
        if (data.Tags?.Count > 0) asset.SetTags(data.Tags);
        mediaRepo.Update(asset);
    }

    private static MediaAsset BuildMediaAsset(TenantId tenantId, SiteId siteId, MediaMetadataPackageData data)
    {
        var fileSizeBytes = data.FileSize > 0 ? data.FileSize : 1;
        var metadata = AssetMetadata.Create(data.FileName, data.ContentType, fileSizeBytes, data.Width, data.Height);
        var asset = MediaAsset.Create(tenantId, siteId, metadata, storageKey: data.Url, uploadedBy: Guid.Empty);
        if (!string.IsNullOrWhiteSpace(data.AltText))
            asset.UpdateAltText(data.AltText);
        return asset;
    }

    private async Task<ImportStepResult> ImportComponentsAsync(
 IReadOnlyList<ComponentPackageData> items, TenantId tenantId, SiteId siteId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await componentRepo.ListAsync(new AllComponentsBySiteSpec(siteId), ct);
        var existingByKey = existing.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                var category = data.Category ?? "Content";
                var zones = data.Zones?.ToList() ?? [];

                if (existingByKey.TryGetValue(data.Key, out var existingComponent))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    existingComponent.Update(data.Name, data.Description, category, zones);
                    componentRepo.Update(existingComponent);
                    overwritten++;
                }
                else
                {
                    var component = Component.Create(tenantId, siteId, data.Name, data.Key,
                   data.Description, category, zones);
                    await componentRepo.AddAsync(component, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Key}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Components", imported, skipped, overwritten, failed, errors);
    }

    private async Task<ImportStepResult> ImportUsersAsync(
     IReadOnlyList<UserPackageData> items, TenantId tenantId,
        ConflictResolution resolution, CancellationToken ct)
    {
        var existing = await userRepo.ListAsync(new AllUsersPagedSpec(1, 10_000), ct);
        var existingByEmail = existing.ToDictionary(u => u.Email.Value, StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0, overwritten = 0, failed = 0;
        var errors = new List<string>();

        foreach (var data in items)
        {
            try
            {
                if (existingByEmail.TryGetValue(data.Email, out var existingUser))
                {
                    if (resolution == ConflictResolution.Skip) { skipped++; continue; }
                    existingUser.UpdateDisplayName(PersonName.Create(data.DisplayName));
                    userRepo.Update(existingUser);
                    overwritten++;
                }
                else
                {
                    var email = EmailAddress.Create(data.Email);
                    var name = PersonName.Create(data.DisplayName);
                    var user = User.Create(tenantId, email, name);
                    await userRepo.AddAsync(user, ct);
                    imported++;
                }
            }
            catch (Exception ex) { failed++; errors.Add($"{data.Email}: {ex.Message}"); }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportStepResult("Users", imported, skipped, overwritten, failed, errors);
    }

    // ── Domain → Package DTO mappers ──────────────────────────────────────

    private static void AddFieldsToContentType(ContentTypeAlias ct, IEnumerable<FieldPackageData> fields)
    {
        foreach (var f in fields)
        {
            if (Enum.TryParse<FieldType>(f.FieldType, out var ft))
                ct.AddField(f.Handle, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsUnique,
               f.Description, f.ValidationJson, f.IsIndexed, f.IsList);
        }
    }

    private static ContentTypePackageData MapContentType(ContentTypeAlias ct) => new(
        Id: ct.Id.Value, Handle: ct.Handle, DisplayName: ct.DisplayName,
        Description: ct.Description, LocalizationMode: ct.LocalizationMode.ToString(),
     Status: ct.Status.ToString(), Kind: ct.Kind.ToString(),
        LayoutId: ct.LayoutId?.Value, CreatedAt: ct.CreatedAt, UpdatedAt: ct.UpdatedAt,
        Fields: ct.Fields.Select(f => new FieldPackageData(
    Id: f.Id, Handle: f.Handle, Label: f.Label,
   FieldType: f.FieldType.ToString(), IsRequired: f.IsRequired,
         IsLocalized: f.IsLocalized, IsUnique: f.IsUnique,
          IsIndexed: f.IsIndexed, IsList: f.IsList, SortOrder: f.SortOrder,
    Description: f.Description, ValidationJson: f.ValidationJson)).ToList());

    private static EntryPackageData MapEntry(Entry e) => new(
        Id: e.Id.Value, ContentTypeId: e.ContentTypeId.Value, ContentTypeHandle: string.Empty,
        Slug: e.Slug.Value, Locale: e.Locale.Value, Status: e.Status.ToString(),
        CurrentVersionNumber: e.CurrentVersionNumber, FieldsJson: e.FieldsJson,
        CreatedAt: e.CreatedAt, UpdatedAt: e.UpdatedAt, PublishedAt: e.PublishedAt);

    private static PagePackageData MapPage(Page p) => new(
   Id: p.Id.Value, Title: p.Title, Slug: p.Slug.Value, PageType: p.PageType.ToString(),
 ParentId: p.ParentId?.Value, Depth: p.Depth,
 LinkedEntryId: p.LinkedEntryId?.Value, CollectionContentTypeId: p.CollectionContentTypeId?.Value,
        RoutePattern: p.RoutePattern, LayoutId: p.LayoutId?.Value,
        CreatedAt: p.CreatedAt, UpdatedAt: p.UpdatedAt);

    private static LayoutPackageData MapLayout(Layout l) => new(
        Id: l.Id.Value, Name: l.Name, Key: l.Key, TemplateType: l.TemplateType.ToString(),
        IsDefault: l.IsDefault, ZonesJson: l.ZonesJson,
        DefaultPlacementsJson: l.DefaultPlacementsJson, ShellTemplate: l.ShellTemplate,
   CreatedAt: l.CreatedAt, UpdatedAt: l.UpdatedAt);

    private static MediaMetadataPackageData MapMedia(MediaAsset m) => new(
        Id: m.Id.Value, FileName: m.Metadata.FileName, ContentType: m.Metadata.MimeType,
     MediaType: m.Metadata.IsImage ? "Image" : m.Metadata.IsVideo ? "Video" : "Document",
        Url: m.StorageKey, FileSize: m.Metadata.SizeBytes,
        Width: m.Metadata.WidthPx, Height: m.Metadata.HeightPx,
        AltText: m.AltText, Tags: m.Tags?.ToList() ?? [],
        CreatedAt: m.CreatedAt);

    private static ComponentPackageData MapComponent(Component c) => new(
    Id: c.Id.Value, Name: c.Name, Key: c.Key, Description: c.Description,
        Category: c.Category, Zones: c.ZonesJson is not null
       ? JsonSerializer.Deserialize<List<string>>(c.ZonesJson) ?? [] : [],
        TemplateType: c.TemplateType.ToString(), TemplateContent: c.TemplateContent,
        Fields: c.Fields.Select(f => new ComponentFieldPackageData(
            Id: f.Id, Handle: f.Handle, Label: f.Label,
     FieldType: f.FieldType.ToString(), IsRequired: f.IsRequired,
       IsLocalized: f.IsLocalized, IsIndexed: f.IsIndexed,
            SortOrder: f.SortOrder, Description: f.Description)).ToList(),
        CreatedAt: c.CreatedAt, UpdatedAt: c.UpdatedAt);

    private static UserPackageData MapUser(User u) => new(
    Id: u.Id.Value, Email: u.Email.Value, DisplayName: u.DisplayName.Value,
     Roles: u.Roles.Select(r => r.Name).ToList(),
   IsActive: u.IsActive, CreatedAt: u.CreatedAt);

    // ── Internal record ───────────────────────────────────────────────────

    private sealed record PackageData(
            PackageManifest Manifest,
            List<ContentTypePackageData> ContentTypes,
            List<EntryPackageData> Entries,
            List<PagePackageData> Pages,
            List<LayoutPackageData> Layouts,
            List<MediaMetadataPackageData> Media,
            List<ComponentPackageData> Components,
            List<UserPackageData> Users);
}
