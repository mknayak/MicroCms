using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Identity;

/// <summary>
/// BCrypt password hasher (work factor 12).
/// BCrypt.Net-Next automatically uses a random salt on each hash;
/// verification is constant-time to prevent timing attacks.
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc/>
    public string Hash(string plainTextPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainTextPassword, nameof(plainTextPassword));
        return BCrypt.Net.BCrypt.EnhancedHashPassword(plainTextPassword, WorkFactor);
    }

    /// <inheritdoc/>
    public bool Verify(string plainTextPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrEmpty(hashedPassword))
            return false;

        return BCrypt.Net.BCrypt.EnhancedVerify(plainTextPassword, hashedPassword);
    }
}
