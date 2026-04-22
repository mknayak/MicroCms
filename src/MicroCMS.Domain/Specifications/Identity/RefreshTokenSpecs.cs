using MicroCMS.Domain.Aggregates.Identity;

namespace MicroCMS.Domain.Specifications.Identity;

/// <summary>Finds a refresh token record by its SHA-256 hash.</summary>
public sealed class RefreshTokenByHashSpec : BaseSpecification<RefreshToken>
{
    public RefreshTokenByHashSpec(string tokenHash)
        : base(t => t.TokenHash == tokenHash)
    {
    }
}

/// <summary>Finds all active (non-revoked, non-expired) refresh tokens belonging to a rotation family.</summary>
public sealed class ActiveTokensByFamilySpec : BaseSpecification<RefreshToken>
{
    public ActiveTokensByFamilySpec(Guid familyId)
        : base(t => t.FamilyId == familyId && !t.IsRevoked)
    {
    }
}

/// <summary>Finds all non-revoked refresh tokens for a given user (used on logout-all-devices).</summary>
public sealed class ActiveTokensByUserSpec : BaseSpecification<RefreshToken>
{
    public ActiveTokensByUserSpec(MicroCMS.Shared.Ids.UserId userId)
        : base(t => t.UserId == userId && !t.IsRevoked)
    {
    }
}
