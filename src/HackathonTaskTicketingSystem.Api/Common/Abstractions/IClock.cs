namespace HackathonTaskTicketingSystem.Common.Abstractions;

/// <summary>
/// Abstraction over the system clock so time-dependent logic (token expiry,
/// timestamps) stays testable.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
