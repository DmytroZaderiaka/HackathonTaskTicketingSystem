namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// An application user authenticating with local email + password credentials.
/// The email is stored trimmed and lower-cased so uniqueness is case-insensitive.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Normalized (trimmed, lower-cased) email address. Unique.</summary>
    public required string Email { get; set; }

    /// <summary>Argon2id password hash; never the plaintext password.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Whether the email address has been verified. Login is blocked until true.</summary>
    public bool IsEmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<EmailVerificationToken> VerificationTokens { get; set; }
        = new List<EmailVerificationToken>();
}
