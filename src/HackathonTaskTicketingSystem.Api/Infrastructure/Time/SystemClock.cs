using HackathonTaskTicketingSystem.Common.Abstractions;

namespace HackathonTaskTicketingSystem.Infrastructure.Time;

/// <inheritdoc />
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
