namespace HackathonTaskTicketingSystem.Domain.Enums;

/// <summary>
/// Ticket classification. Serialized to the canonical API values (bug|feature|fix).
/// </summary>
public enum TicketType
{
    Bug,
    Feature,
    Fix,
}
