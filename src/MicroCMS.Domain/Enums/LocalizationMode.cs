namespace MicroCMS.Domain.Enums;

/// <summary>
/// Controls how entry field values are stored across locales for a content type.
/// </summary>
public enum LocalizationMode
{
    /// <summary>
    /// Each locale has an independent entry record.
    /// Fields marked <c>IsLocalized = true</c> store distinct values per locale;
    /// non-localized fields are shared via the default-locale entry.
    /// </summary>
    PerLocale = 0,

    /// <summary>
    /// A single entry record is shared across all locales.
    /// No field-level locale variants are created.
    /// Suitable for taxonomy, configuration, or singleton content types.
    /// </summary>
    Shared = 1
}
