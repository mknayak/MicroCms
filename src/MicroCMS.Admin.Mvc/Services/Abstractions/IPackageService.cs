namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IPackageService
{
    Task<byte[]> ExportAsync(string tenantId, string? siteId = null, CancellationToken ct = default);
    Task ImportAsync(Stream packageStream, string fileName, CancellationToken ct = default);
}
