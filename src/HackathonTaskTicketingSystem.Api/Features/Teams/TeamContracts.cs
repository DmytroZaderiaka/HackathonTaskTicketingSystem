using System.ComponentModel.DataAnnotations;

namespace HackathonTaskTicketingSystem.Features.Teams;

public sealed record CreateTeamRequest([Required, MaxLength(200)] string Name);

public sealed record UpdateTeamRequest([Required, MaxLength(200)] string Name);

public sealed record TeamResponse(Guid Id, string Name, DateTime CreatedAt, DateTime ModifiedAt);

public enum CreateTeamOutcome
{
    Success,
    EmptyName,
    NameConflict,
}

public enum UpdateTeamOutcome
{
    Success,
    EmptyName,
    NotFound,
    NameConflict,
}

public enum DeleteTeamOutcome
{
    Success,
    NotFound,
    // Blocked (409) is added in later phases once epics/tickets can reference a team.
}
