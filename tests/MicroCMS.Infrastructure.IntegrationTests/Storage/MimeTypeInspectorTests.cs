using FluentAssertions;
using MicroCMS.Infrastructure.Storage.Mime;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Storage;

public sealed class MimeTypeInspectorTests
{
    private readonly MimeTypeInspector _inspector = new();

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "photo.jpg", "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image.png", "image/png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "anim.gif", "image/gif")]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, "document.pdf", "application/pdf")]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "archive.zip", "application/zip")]
    public async Task DetectAsync_ShouldIdentifyMimeFromMagicBytes(
        byte[] magicBytes, string fileName, string expectedMime)
    {
        using var stream = new MemoryStream(magicBytes);
        var result = await _inspector.DetectAsync(stream, fileName);
        result.Should().Be(expectedMime);
    }

    [Fact]
    public async Task DetectAsync_ShouldFallBackToExtension_WhenMagicBytesUnknown()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var result = await _inspector.DetectAsync(stream, "file.mp3");
        result.Should().Be("audio/mpeg");
    }

    [Fact]
    public async Task DetectAsync_ShouldReturnOctetStream_WhenUnknown()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var result = await _inspector.DetectAsync(stream, "file.unknown");
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task DetectAsync_ShouldResetStreamPosition_WhenStreamIsSeekable()
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 });
        stream.Position = 0;

        await _inspector.DetectAsync(stream, "photo.jpg");

        // Stream position must be reset so the caller can still read the file
        stream.Position.Should().Be(0);
    }

    [Fact]
    public async Task DetectAsync_ShouldIdentifyWebP()
    {
        // RIFF????WEBP — padding bytes in positions 4-7
        var webp = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, // "RIFF"
            0x24, 0x00, 0x00, 0x00, // file size (little-endian)
            0x57, 0x45, 0x42, 0x50  // "WEBP"
        };
        using var stream = new MemoryStream(webp);
        var result = await _inspector.DetectAsync(stream, "image.webp");
        result.Should().Be("image/webp");
    }
}
