using Amazon.S3;
using Amazon.S3.Model;
using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.S3;

/// <summary>
/// Stores assets in an AWS S3-compatible bucket (also works with MinIO).
/// Storage key format: <c>{tenantId}/{year}/{month}/{guid}_{fileName}</c>.
/// </summary>
public sealed class S3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;

    public S3StorageProvider(IAmazonS3 s3, IOptions<S3StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(tenantId, fileName);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_options.BucketName, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken);
    }

    public Task<string> GetPublicUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!_options.PublicAccess)
            return Task.FromResult(string.Empty);

        var url = $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{storageKey}";
        return Task.FromResult(url);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_options.BucketName, storageKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildKey(string tenantId, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        return $"{tenantId}/{now.Year}/{now.Month:D2}/{Guid.NewGuid():N}_{SanitiseKey(fileName)}";
    }

    private static string SanitiseKey(string fileName)
    {
        var safe = string.Concat(fileName.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
        return safe.Length > 100 ? safe[^100..] : safe;
    }
}
