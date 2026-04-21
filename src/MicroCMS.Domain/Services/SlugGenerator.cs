using System.Text;
using System.Text.RegularExpressions;
using MicroCMS.Domain.ValueObjects;

namespace MicroCMS.Domain.Services;

/// <summary>
/// Domain service that converts arbitrary human-readable text into URL-safe <see cref="Slug"/> values.
/// Security: uses a strict whitelist — only lowercase alphanumerics and hyphens survive.
/// Non-ASCII characters are normalised to their closest ASCII equivalent via Unicode decomposition
/// before being stripped, so "Ünïcödé" → "unicode" rather than producing empty output.
/// </summary>
public sealed class SlugGenerator
{
    private static readonly Regex MultipleHyphens =
        new(@"-{2,}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex AllowedChars =
        new(@"[^a-z0-9-]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Generates a slug from <paramref name="text"/>.
    /// Throws <see cref="Domain.Exceptions.DomainException"/> if the result is empty or exceeds max length.
    /// </summary>
    public Slug Generate(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        var normalised = Normalise(text);
        var trimmed = normalised.Trim('-');

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new Exceptions.DomainException(
                $"Cannot generate a slug from '{text}' — no valid characters remain after normalisation.");
        }

        // Truncate at word boundary if possible
        var truncated = trimmed.Length > Slug.MaxLength
            ? TruncateAtWordBoundary(trimmed, Slug.MaxLength)
            : trimmed;

        return Slug.Create(truncated);
    }

    /// <summary>
    /// Returns a candidate slug without throwing if the source text produces invalid output.
    /// Returns null when generation is not possible.
    /// </summary>
    public Slug? TryGenerate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return Generate(text);
        }
        catch
        {
            return null;
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static string Normalise(string text)
    {
        // Decompose Unicode characters (é → e + combining accent)
        var decomposed = text.Normalize(NormalizationForm.FormD);

        // Build output keeping only ASCII chars, converting spaces/punctuation to hyphens
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);

            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue; // drop combining diacritics
            }

            if (char.IsLetterOrDigit(c) && c < 128)
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSeparator(c))
            {
                sb.Append('-');
            }
        }

        var raw = sb.ToString();

        // Collapse consecutive hyphens
        return MultipleHyphens.Replace(raw, "-");
    }

    private static string TruncateAtWordBoundary(string value, int maxLength)
    {
        var truncated = value[..maxLength];
        var lastHyphen = truncated.LastIndexOf('-');
        return lastHyphen > 0 ? truncated[..lastHyphen] : truncated;
    }
}
