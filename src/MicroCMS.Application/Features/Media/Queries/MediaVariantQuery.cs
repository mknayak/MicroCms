using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Media.Queries;

/// <summary>
/// Retrieves a transformed variant of an image asset.
/// Returns a <see cref="ImageVariantResult"/> containing the output stream and resolved MIME type.
///
/// Marked <see cref="AllowAnonymousRequestAttribute"/> so browser <c>&lt;img&gt;</c> tags can load
/// thumbnails without a bearer token. The controller action is also <c>[AllowAnonymous]</c>.
/// </summary>
[AllowAnonymousRequest]
public sealed record GetImageVariantQuery(
    Guid AssetId,
    int? Width,
    int? Height,
    ImageFit Fit = ImageFit.Contain,
    ImageOutputFormat Format = ImageOutputFormat.Original,
    int Quality = 85) : IQuery<ImageVariantResult>;

/// <summary>Carries the output stream and MIME type for an image variant response.</summary>
public sealed record ImageVariantResult(Stream Content, string MimeType);
