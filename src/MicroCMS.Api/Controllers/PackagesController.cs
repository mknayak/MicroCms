using MicroCMS.Application.Features.PackageManager.Commands;
using MicroCMS.Application.Features.PackageManager.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Package Manager — export and import full-site data packages as ZIP archives.
/// </summary>
[Authorize]
public sealed class PackagesController : ApiControllerBase
{
    // ── Export ────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports selected artefacts for a site/tenant as a downloadable ZIP.
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromBody] ExportOptions options,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ExportPackageCommand(options), ct);
        if (result.IsFailure) return ToProblemResult(result.Error);

  var payload = result.Value;
        return File(payload.ZipBytes, "application/zip", payload.FileName);
    }

    // ── Analyse ───────────────────────────────────────────────────────────

    /// <summary>
    /// Reads an uploaded package ZIP and returns analysis stats without applying any changes.
    /// </summary>
  [HttpPost("analyse")]
    [ProducesResponseType(typeof(PackageAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(512 * 1024 * 1024)] // 512 MB max upload
    public async Task<IActionResult> Analyse(
        [FromForm] IFormFile file,
        [FromQuery] Guid targetTenantId,
        [FromQuery] Guid targetSiteId,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
      return BadRequest(new { detail = "No file uploaded." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var zipBytes = ms.ToArray();

        var result = await Sender.Send(
            new AnalysePackageCommand(zipBytes, targetTenantId, targetSiteId), ct);

        return result.IsSuccess ? Ok(result.Value) : ToProblemResult(result.Error);
    }

    // ── Import ────────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads and applies a package ZIP to the target site.
    /// Returns import progress / summary.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportProgress), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(512 * 1024 * 1024)]
    public async Task<IActionResult> Import(
        [FromForm] IFormFile file,
        [FromQuery] Guid targetTenantId,
   [FromQuery] Guid targetSiteId,
        [FromForm] bool importContentTypes = true,
        [FromForm] bool importEntries = true,
 [FromForm] bool importPages = true,
        [FromForm] bool importLayouts = true,
        [FromForm] bool importMediaMetadata = true,
        [FromForm] bool importComponents = true,
     [FromForm] bool importUsers = false,
        [FromForm] bool importSiteSettings = true,
        [FromForm] string conflictResolution = "Skip",
        CancellationToken ct = default)
 {
        if (file is null || file.Length == 0)
     return BadRequest(new { detail = "No file uploaded." });

        var resolution = string.Equals(conflictResolution, "Overwrite", StringComparison.OrdinalIgnoreCase)
  ? ConflictResolution.Overwrite
  : ConflictResolution.Skip;

        var opts = new ImportOptions(
   ImportContentTypes: importContentTypes,
            ImportEntries: importEntries,
       ImportPages: importPages,
      ImportLayouts: importLayouts,
            ImportMediaMetadata: importMediaMetadata,
            ImportComponents: importComponents,
            ImportUsers: importUsers,
         ImportSiteSettings: importSiteSettings,
            ConflictResolution: resolution);

   using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var zipBytes = ms.ToArray();

  var result = await Sender.Send(
    new ImportPackageCommand(zipBytes, targetTenantId, targetSiteId, opts), ct);

      return result.IsSuccess ? Ok(result.Value) : ToProblemResult(result.Error);
    }
}
