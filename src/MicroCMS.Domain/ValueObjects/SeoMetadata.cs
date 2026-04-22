using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// SEO metadata attached to an entry (GAP-08).
/// MetaTitle ≤ 60 chars, MetaDescription ≤ 160 chars — mirrors the SERP preview UI.
/// </summary>
public sealed class SeoMetadata : ValueObject
{
    public const int MaxMetaTitleLength = 60;
    public const int MaxMetaDescriptionLength = 160;

    private SeoMetadata() { } // EF Core

    private SeoMetadata(string? metaTitle, string? metaDescription, string? canonicalUrl, string? ogImage)
    {
        MetaTitle = metaTitle;
      MetaDescription = metaDescription;
        CanonicalUrl = canonicalUrl;
        OgImage = ogImage;
    }

    public string? MetaTitle { get; private set; }
  public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgImage { get; private set; }

    public static SeoMetadata Create(
     string? metaTitle,
     string? metaDescription,
        string? canonicalUrl = null,
        string? ogImage = null)
    {
        if (metaTitle is not null && metaTitle.Length > MaxMetaTitleLength)
   throw new DomainException($"MetaTitle must not exceed {MaxMetaTitleLength} characters.");

   if (metaDescription is not null && metaDescription.Length > MaxMetaDescriptionLength)
   throw new DomainException($"MetaDescription must not exceed {MaxMetaDescriptionLength} characters.");

  return new SeoMetadata(
            metaTitle?.Trim(),
            metaDescription?.Trim(),
            canonicalUrl?.Trim(),
            ogImage?.Trim());
    }

    public static SeoMetadata Empty => new(null, null, null, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
 yield return MetaTitle;
        yield return MetaDescription;
        yield return CanonicalUrl;
        yield return OgImage;
    }
}
