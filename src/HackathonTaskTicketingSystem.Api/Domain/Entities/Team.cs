using HackathonTaskTicketingSystem.Common.Abstractions;

namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// A team that groups epics and tickets. The name is unique case-insensitively while its
/// original casing is preserved for display (via <see cref="NormalizedName"/>).
/// </summary>
public class Team : IAuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Display name, trimmed.</summary>
    public required string Name { get; set; }

    /// <summary>Upper-cased name used to enforce case-insensitive uniqueness.</summary>
    public required string NormalizedName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }
}
