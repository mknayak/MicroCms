using System.Security.Cryptography;
using System.Text;
using MicroCMS.Application.Features.ApiClients.Commands;

namespace MicroCMS.Infrastructure.Identity;

/// <summary>
/// SHA-256 secret hasher for API client keys.
/// API keys are stored as SHA-256 hex digests; the raw key is shown exactly once.
/// SHA-256 is appropriate here (rather than bcrypt) because API keys are long
/// random strings (256 bits of entropy) — not user-memorable passwords —
/// so dictionary / brute-force attacks are computationally infeasible.
/// </summary>
internal sealed class Sha256SecretHasher : ISecretHasher
{
    /// <inheritdoc/>
    public string Hash(string rawSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawSecret, nameof(rawSecret));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public bool Verify(string rawSecret, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawSecret) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var computed = Hash(rawSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
