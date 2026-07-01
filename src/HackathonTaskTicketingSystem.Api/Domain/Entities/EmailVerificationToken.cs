namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// A single-use email-verification token. Only the hash of the token is persisted;
/// the raw value lives solely in the verification link sent to the user.
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hash (Base64) of the raw token.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>When the token was consumed; <c>null</c> while still unused.</summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
