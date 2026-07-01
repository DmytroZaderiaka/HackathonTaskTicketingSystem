using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Domain.Enums;

namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// A work ticket on a team board. It may optionally reference an epic, but only one that
/// belongs to the same team.
/// </summary>
public class Ticket : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    /// <summary>Optional epic; when set, must belong to <see cref="TeamId"/>.</summary>
    public Guid? EpicId { get; set; }

    public Epic? Epic { get; set; }

    public TicketType Type { get; set; }

    public TicketState State { get; set; }

    /// <summary>Short title, trimmed and non-empty.</summary>
    public required string Title { get; set; }

    /// <summary>Body text, non-empty.</summary>
    public required string Body { get; set; }

    public Guid CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }
}
