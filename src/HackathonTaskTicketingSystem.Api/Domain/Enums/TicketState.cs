namespace HackathonTaskTicketingSystem.Domain.Enums;

/// <summary>
/// Fixed Kanban workflow states. Serialized to the canonical snake_case API values
/// (new | ready_for_implementation | in_progress | ready_for_acceptance | done).
/// </summary>
public enum TicketState
{
    New,
    ReadyForImplementation,
    InProgress,
    ReadyForAcceptance,
    Done,
}
