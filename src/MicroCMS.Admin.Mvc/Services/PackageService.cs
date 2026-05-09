using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class PackageService : ApiClientBase, IPackageService
{
    public PackageService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<byte[]> ExportAsync(string tenantId, string? siteId = null, CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(siteId)
            ? $"packages/export?tenantId={Uri.EscapeDataString(tenantId)}"
            : $"packages/export?tenantId={Uri.EscapeDataString(tenantId)}&siteId={Uri.EscapeDataString(siteId)}";

        return GetBytesAsync(path, ct);
    }

    public async Task ImportAsync(Stream packageStream, string fileName, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(packageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        form.Add(streamContent, "file", fileName);

        await PostMultipartAsync<object>("packages/import", form, ct);
    }
}
