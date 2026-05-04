using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Redacts personally identifiable information (PII) from text before sending to AI providers.
/// Reads the enablement flag from <see cref="ISettingsReader"/> using <see cref="AiSettingKeys.PiiRedactionEnabled"/>.
/// GAP-28: PII protection.
/// </summary>
public sealed class PiiRedactor
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<PiiRedactor> _logger;

    private static readonly Regex SsnPattern =
        new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    private static readonly Regex EmailPattern =
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);

    private static readonly Regex CreditCardPattern =
        new(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.Compiled);

    private static readonly Regex PhonePattern =
        new(@"\b\+?1?\s?\(?\d{3}\)?[\s.\-]?\d{3}[\s.\-]?\d{4}\b", RegexOptions.Compiled);

    private static readonly Regex AddressPattern =
        new(@"\b\d{1,5}\s\w+(\s\w+)*\s(Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Court|Ct|Circle|Cir|Way)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PiiRedactor(ISettingsReader settingsReader, ILogger<PiiRedactor> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    /// <summary>
    /// Redacts PII from the input text if redaction is enabled for the tenant/site.
    /// Returns the original text if redaction is disabled.
    /// </summary>
    public async Task<string> RedactAsync(
        TenantId tenantId,
        SiteId? siteId,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Check if PII redaction is enabled
        var isEnabled = await _settingsReader.GetAsync<bool>(
            tenantId,
            siteId,
            AiSettingKeys.PiiRedactionEnabled,
            defaultValue: false,
            cancellationToken);

        if (!isEnabled)
        {
            return text;
        }

        _logger.LogDebug("PII redaction enabled for tenant {TenantId}, site {SiteId}", tenantId, siteId);

        var redactedText = text;
        var redactionCount = 0;

        // Redact SSN
        redactedText = SsnPattern.Replace(redactedText, match =>
        {
            redactionCount++;
            return "[REDACTED_SSN]";
        });

        // Redact email addresses
        redactedText = EmailPattern.Replace(redactedText, match =>
        {
            redactionCount++;
            return "[REDACTED_EMAIL]";
        });

        // Redact credit card numbers
        redactedText = CreditCardPattern.Replace(redactedText, match =>
        {
            redactionCount++;
            return "[REDACTED_CREDIT_CARD]";
        });

        // Redact phone numbers
        redactedText = PhonePattern.Replace(redactedText, match =>
        {
            redactionCount++;
            return "[REDACTED_PHONE]";
        });

        // Redact street addresses
        redactedText = AddressPattern.Replace(redactedText, match =>
        {
            redactionCount++;
            return "[REDACTED_ADDRESS]";
        });

        if (redactionCount > 0)
        {
            _logger.LogInformation(
                "Redacted {Count} PII instances from text for tenant {TenantId}, site {SiteId}",
                redactionCount,
                tenantId,
                siteId);
        }

        return redactedText;
    }

    /// <summary>
    /// Redacts PII from multiple text segments.
    /// </summary>
    public async Task<List<string>> RedactManyAsync(
        TenantId tenantId,
        SiteId? siteId,
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var tasks = texts.Select(text => RedactAsync(tenantId, siteId, text, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
