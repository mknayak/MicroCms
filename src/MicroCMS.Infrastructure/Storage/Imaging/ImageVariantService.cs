using MicroCMS.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MicroCMS.Infrastructure.Storage.Imaging;

/// <summary>
/// On-demand image resizing, cropping, and format conversion using SixLabors.ImageSharp.
/// All operations are performed in-memory; the output stream is owned by the caller.
/// </summary>
public sealed class ImageVariantService : IImageVariantService
{
    public async Task<Stream> TransformAsync(
        Stream source,
        ImageVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        // ImageSharp's WithSeekableStreamAsync requires a fully in-memory, seekable stream.
        // Storage providers may return FileStream (useAsync:true) or other non-rewindable
        // streams that cause UnknownImageFormatException even for valid formats.
        // Buffering here is safe: variants are only generated for image files, which are
        // already size-constrained by the asset pipeline.
        MemoryStream buffered;
        if (source is MemoryStream ms && ms.CanSeek)
        {
            buffered = ms;
        }
        else
        {
            buffered = new MemoryStream();
            await source.CopyToAsync(buffered, cancellationToken);
            await source.DisposeAsync();
        }
        buffered.Position = 0;

        using var image = await Image.LoadAsync(buffered, cancellationToken);

        ApplyResize(image, request);

        var output = new MemoryStream();
        var encoder = ResolveEncoder(request.Format, request.Quality, image.Metadata.DecodedImageFormat);
        await image.SaveAsync(output, encoder, cancellationToken);
        output.Position = 0;
        return output;
    }

    public string GetMimeType(ImageOutputFormat format, string sourceMimeType) => format switch
    {
        ImageOutputFormat.Jpeg => "image/jpeg",
        ImageOutputFormat.Png  => "image/png",
        ImageOutputFormat.WebP => "image/webp",
        _                      => sourceMimeType
    };

    // ── Private helpers ───────────────────────────────────────────────────

    private static void ApplyResize(Image image, ImageVariantRequest request)
    {
        if (request.Width is null && request.Height is null)
            return;

        var resizeMode = request.Fit switch
        {
            ImageFit.Cover   => ResizeMode.Crop,
            ImageFit.Fill    => ResizeMode.Stretch,
            _                => ResizeMode.Max
        };

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(request.Width ?? 0, request.Height ?? 0),
            Mode = resizeMode
        }));
    }

    private static IImageEncoder ResolveEncoder(
        ImageOutputFormat format,
        int quality,
        IImageFormat? sourceFormat)
    {
        return format switch
        {
            ImageOutputFormat.Jpeg => new JpegEncoder { Quality = quality },
            ImageOutputFormat.Png  => new PngEncoder(),
            ImageOutputFormat.WebP => new WebpEncoder { Quality = quality },
            _ => GetEncoderForFormat(sourceFormat, quality)
        };
    }

    private static IImageEncoder GetEncoderForFormat(IImageFormat? format, int quality)
    {
        if (format is JpegFormat)
            return new JpegEncoder { Quality = quality };
        if (format is PngFormat)
            return new PngEncoder();
        if (format is WebpFormat)
            return new WebpEncoder { Quality = quality };
        return new JpegEncoder { Quality = quality };
    }
}
