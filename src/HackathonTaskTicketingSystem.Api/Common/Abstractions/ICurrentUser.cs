namespace HackathonTaskTicketingSystem.Common.Abstractions;

/// <summary>
/// Exposes the authenticated user for the current request, resolved from the
/// authentication cookie principal.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}
