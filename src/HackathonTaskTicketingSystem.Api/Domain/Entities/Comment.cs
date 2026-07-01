namespace HackathonTaskTicketingSystem.Domain.Entities;

/// <summary>
/// A comment on a ticket. Immutable in the mandatory scope. Adding a comment does not
/// change the ticket's <c>ModifiedAt</c> (comments are not auditable entities and the
/// ticket itself is not touched here).
/// </summary>
public class Comment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public User Author { get; set; } = null!;

    /// <summary>Comment body, trimmed and non-empty.</summary>
    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; }
}
