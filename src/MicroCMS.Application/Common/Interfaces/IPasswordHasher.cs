namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Abstracts password hashing so the domain and application layers
/// never take a compile-time dependency on BCrypt.Net or any other
/// concrete hashing library.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Returns a bcrypt hash of <paramref name="plainTextPassword"/>.</summary>
    string Hash(string plainTextPassword);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="plainTextPassword"/> matches
    /// <paramref name="hashedPassword"/>. Constant-time comparison prevents
    /// timing-based side-channel attacks.
    /// </summary>
    bool Verify(string plainTextPassword, string hashedPassword);
}
