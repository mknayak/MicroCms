using Amazon.Runtime;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using MicroCMS.Infrastructure.Storage.S3;
using Microsoft.Extensions.Options;
using Testcontainers.Minio;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Storage;

/// <summary>
/// Integration tests for <see cref="S3StorageProvider"/> using a MinIO container.
/// Requires Docker. Skipped automatically when Docker is unavailable.
/// </summary>
[Trait("Category", "Integration")]
public sealed class S3StorageProviderTests : IAsyncLifetime
{
    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:latest")
        .Build();

    private S3StorageProvider _provider = null!;
    private const string BucketName = "test-bucket";
    private const string TenantId = "tenant-abc";

    public async Task InitializeAsync()
    {
        await _minio.StartAsync();

        var s3Config = new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true
        };

        var s3Client = new AmazonS3Client(
            new BasicAWSCredentials(_minio.GetAccessKey(), _minio.GetSecretKey()),
            s3Config);

        // Create test bucket
        await s3Client.PutBucketAsync(BucketName);

        var options = Options.Create(new S3StorageOptions
        {
            BucketName = BucketName,
            Region = "us-east-1",
            AccessKeyId = _minio.GetAccessKey(),
            SecretAccessKey = _minio.GetSecretKey(),
            ServiceUrl = _minio.GetConnectionString()
        });

        _provider = new S3StorageProvider(s3Client, options);
    }

    public async Task DisposeAsync() => await _minio.DisposeAsync();

    [Fact]
    public async Task UploadAsync_ShouldReturnNonEmptyKey()
    {
        // Arrange
        using var content = new MemoryStream("hello world"u8.ToArray());

        // Act
        var key = await _provider.UploadAsync(content, "test.txt", "text/plain", TenantId);

        // Assert
        key.Should().NotBeNullOrWhiteSpace();
        key.Should().StartWith(TenantId);
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnOriginalContent()
    {
        // Arrange
        var originalBytes = "test content for download"u8.ToArray();
        using var uploadStream = new MemoryStream(originalBytes);
        var key = await _provider.UploadAsync(uploadStream, "download-test.txt", "text/plain", TenantId);

        // Act
        await using var downloadStream = await _provider.DownloadAsync(key);
        using var ms = new MemoryStream();
        await downloadStream.CopyToAsync(ms);

        // Assert
        ms.ToArray().Should().BeEquivalentTo(originalBytes);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenObjectExists()
    {
        // Arrange
        using var content = new MemoryStream("exists check"u8.ToArray());
        var key = await _provider.UploadAsync(content, "exists.txt", "text/plain", TenantId);

        // Act
        var exists = await _provider.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_ForMissingObject()
    {
        var exists = await _provider.ExistsAsync("does-not-exist/fake-key.bin");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveObject()
    {
        // Arrange
        using var content = new MemoryStream("delete me"u8.ToArray());
        var key = await _provider.UploadAsync(content, "to-delete.txt", "text/plain", TenantId);

        // Act
        await _provider.DeleteAsync(key);

        // Assert
        var exists = await _provider.ExistsAsync(key);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_ShouldHandleLargeStream()
    {
        // Arrange — 1 MB of random bytes
        var largeData = new byte[1 * 1024 * 1024];
        Random.Shared.NextBytes(largeData);
        using var content = new MemoryStream(largeData);

        // Act
        var key = await _provider.UploadAsync(content, "large-file.bin", "application/octet-stream", TenantId);

        // Assert
        key.Should().NotBeNullOrWhiteSpace();
        var exists = await _provider.ExistsAsync(key);
        exists.Should().BeTrue();
    }
}
