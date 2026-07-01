using HackathonTaskTicketingSystem.Common.Abstractions;

namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// An epic groups related tickets within a single team. Its team is fixed at creation
/// and cannot be changed afterwards.
/// </summary>
public class Epic : IAuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Owning team. Immutable after creation.</summary>
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    /// <summary>Title, trimmed and non-empty.</summary>
    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }
}
