using FluentAssertions;
using MicroCMS.Infrastructure.Storage.Filesystem;
using Microsoft.Extensions.Options;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Storage;

/// <summary>
/// Fast integration tests for <see cref="FilesystemStorageProvider"/>.
/// Uses a temp directory — no Docker required.
/// </summary>
public sealed class FilesystemStorageProviderTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "MicroCMS_Tests_" + Guid.NewGuid().ToString("N"));
    private readonly FilesystemStorageProvider _provider;

    public FilesystemStorageProviderTests()
    {
        var options = Options.Create(new FilesystemStorageOptions { RootPath = _rootPath });
        _provider = new FilesystemStorageProvider(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFileAndReturnKey()
    {
        using var content = new MemoryStream("hello fs"u8.ToArray());
        var key = await _provider.UploadAsync(content, "test.txt", "text/plain", "tenant1");

        key.Should().StartWith("tenant1");
        var exists = await _provider.ExistsAsync(key);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnOriginalContent()
    {
        var original = "filesystem round-trip"u8.ToArray();
        using var uploadStream = new MemoryStream(original);
        var key = await _provider.UploadAsync(uploadStream, "round-trip.txt", "text/plain", "tenant1");

        await using var download = await _provider.DownloadAsync(key);
        using var ms = new MemoryStream();
        await download.CopyToAsync(ms);

        ms.ToArray().Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        using var content = new MemoryStream("delete me"u8.ToArray());
        var key = await _provider.UploadAsync(content, "to-delete.txt", "text/plain", "tenant1");

        await _provider.DeleteAsync(key);

        var exists = await _provider.ExistsAsync(key);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicUrlAsync_ShouldReturnEmpty_ForFilesystemProvider()
    {
        using var content = new MemoryStream("test"u8.ToArray());
        var key = await _provider.UploadAsync(content, "test.txt", "text/plain", "tenant1");

        var url = await _provider.GetPublicUrlAsync(key);
        url.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldSanitiseSpecialCharactersInFileName()
    {
        using var content = new MemoryStream("test"u8.ToArray());
        var key = await _provider.UploadAsync(content, "file with spaces & <chars>.txt", "text/plain", "tenant1");

        key.Should().NotContain(" ");
        key.Should().NotContain("<");
        key.Should().NotContain(">");
        key.Should().NotContain("&");
    }
}
