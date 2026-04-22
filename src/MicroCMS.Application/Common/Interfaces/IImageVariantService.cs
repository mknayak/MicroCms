namespace MicroCMS.Application.Common.Interfaces;

/// <summary>How the image is resized when both width and height are specified.</summary>
public enum ImageFit
{
    /// <summary>Scale down to fit within the bounding box, preserving aspect ratio.</summary>
    Contain,

    /// <summary>Scale and crop to fill the bounding box exactly.</summary>
    Cover,

    /// <summary>Stretch to fill; does not preserve aspect ratio.</summary>
    Fill
}

/// <summary>Desired output encoding for a transformed image.</summary>
public enum ImageOutputFormat
{
    /// <summary>Keep the source format.</summary>
    Original,
    Jpeg,
    Png,
    WebP
}

/// <summary>Parameters for a single image transform request.</summary>
public sealed record ImageVariantRequest(
    int? Width,
    int? Height,
    ImageFit Fit = ImageFit.Contain,
    ImageOutputFormat Format = ImageOutputFormat.Original,
    int Quality = 85);

/// <summary>
/// On-demand image resizing, cropping, and format conversion.
/// Backed by SixLabors.ImageSharp in the Infrastructure layer.
/// </summary>
public interface IImageVariantService
{
    /// <summary>
    /// Transforms <paramref name="source"/> according to <paramref name="request"/> and
    /// returns the result as a new stream (caller owns and must dispose).
    /// </summary>
    Task<Stream> TransformAsync(
        Stream source,
        ImageVariantRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the MIME type that corresponds to <paramref name="format"/>.</summary>
    string GetMimeType(ImageOutputFormat format, string sourceMimeType);
}
