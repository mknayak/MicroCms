using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.AzureBlob;

/// <summary>
/// Stores assets in an Azure Blob Storage container.
/// Storage key (blob name) format: <c>{tenantId}/{year}/{month}/{guid}_{fileName}</c>.
/// </summary>
public sealed class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _container;
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobStorageProvider(IOptions<AzureBlobStorageOptions> options)
    {
        _options = options.Value;
        var serviceClient = new BlobServiceClient(_options.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(tenantId, fileName);
        var blob = _container.GetBlobClient(key);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = mimeType }
        };

        await blob.UploadAsync(content, uploadOptions, cancellationToken);
        return key;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Task<string> GetPublicUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!_options.PublicAccess)
            return Task.FromResult(string.Empty);

        var uri = _container.GetBlobClient(storageKey).Uri.ToString();
        return Task.FromResult(uri);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(storageKey);
        var response = await blob.ExistsAsync(cancellationToken);
        return response.Value;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildKey(string tenantId, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        var safe = string.Concat(fileName.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
        var trimmed = safe.Length > 100 ? safe[^100..] : safe;
        return $"{tenantId}/{now.Year}/{now.Month:D2}/{Guid.NewGuid():N}_{trimmed}";
    }
}
