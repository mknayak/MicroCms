using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.PackageManager.Commands;
using MicroCMS.Application.Features.PackageManager.Dtos;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.PackageManager.Handlers;

// ─── Export ───────────────────────────────────────────────────────────────────

internal sealed class ExportPackageCommandHandler(IPackageService packageService)
    : IRequestHandler<ExportPackageCommand, Result<ExportPackageResult>>
{
    public async Task<Result<ExportPackageResult>> Handle(
        ExportPackageCommand request,
        CancellationToken cancellationToken)
    {
        var zipBytes = await packageService.ExportAsync(request.Options, cancellationToken);
        var ts = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = $"mcms-export-{request.Options.SiteId:N}-{ts}.zip";
        return Result.Success(new ExportPackageResult(zipBytes, fileName));
    }
}

// ─── Analyse ──────────────────────────────────────────────────────────────────

internal sealed class AnalysePackageCommandHandler(IPackageService packageService)
    : IRequestHandler<AnalysePackageCommand, Result<PackageAnalysisResult>>
{
    public async Task<Result<PackageAnalysisResult>> Handle(
        AnalysePackageCommand request,
        CancellationToken cancellationToken)
    {
        var result = await packageService.AnalyseAsync(
                            request.ZipBytes,
                            request.TargetTenantId,
                            request.TargetSiteId,
                            cancellationToken);

        return Result.Success(result);
    }
}

// ─── Import ───────────────────────────────────────────────────────────────────

internal sealed class ImportPackageCommandHandler(IPackageService packageService)
    : IRequestHandler<ImportPackageCommand, Result<ImportProgress>>
{
    public async Task<Result<ImportProgress>> Handle(
        ImportPackageCommand request,
        CancellationToken cancellationToken)
    {
        var progress = await packageService.ImportAsync(
                        request.ZipBytes,
                        request.TargetTenantId,
                        request.TargetSiteId,
                        request.Options,
                        cancellationToken);

        return Result.Success(progress);
    }
}
