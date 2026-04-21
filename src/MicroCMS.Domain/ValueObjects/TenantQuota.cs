using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Per-tenant resource quotas (FR-MT-5).
/// All values are upper bounds; zero means "no limit".
/// </summary>
public sealed class TenantQuota : ValueObject
{
    private TenantQuota(
        long maxStorageBytes,
        int maxApiCallsPerMinute,
        int maxUsers,
        int maxSites,
        int maxContentTypes,
        long maxAiTokensPerMonth)
    {
        MaxStorageBytes = maxStorageBytes;
        MaxApiCallsPerMinute = maxApiCallsPerMinute;
        MaxUsers = maxUsers;
        MaxSites = maxSites;
        MaxContentTypes = maxContentTypes;
        MaxAiTokensPerMonth = maxAiTokensPerMonth;
    }

    public long MaxStorageBytes { get; }
    public int MaxApiCallsPerMinute { get; }
    public int MaxUsers { get; }
    public int MaxSites { get; }
    public int MaxContentTypes { get; }
    public long MaxAiTokensPerMonth { get; }

    public static TenantQuota Default => Create(
        maxStorageBytes: 10L * 1024 * 1024 * 1024,  // 10 GB
        maxApiCallsPerMinute: 600,
        maxUsers: 25,
        maxSites: 5,
        maxContentTypes: 50,
        maxAiTokensPerMonth: 1_000_000);

    public static TenantQuota Unlimited => Create(0, 0, 0, 0, 0, 0);

    public static TenantQuota Create(
        long maxStorageBytes,
        int maxApiCallsPerMinute,
        int maxUsers,
        int maxSites,
        int maxContentTypes,
        long maxAiTokensPerMonth)
    {
        if (maxStorageBytes < 0)
        {
            throw new DomainException("MaxStorageBytes must be non-negative.");
        }

        if (maxApiCallsPerMinute < 0)
        {
            throw new DomainException("MaxApiCallsPerMinute must be non-negative.");
        }

        return new TenantQuota(
            maxStorageBytes,
            maxApiCallsPerMinute,
            maxUsers,
            maxSites,
            maxContentTypes,
            maxAiTokensPerMonth);
    }

    public bool IsStorageUnlimited => MaxStorageBytes == 0;
    public bool IsApiRateLimitUnlimited => MaxApiCallsPerMinute == 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxStorageBytes;
        yield return MaxApiCallsPerMinute;
        yield return MaxUsers;
        yield return MaxSites;
        yield return MaxContentTypes;
        yield return MaxAiTokensPerMonth;
    }
}
