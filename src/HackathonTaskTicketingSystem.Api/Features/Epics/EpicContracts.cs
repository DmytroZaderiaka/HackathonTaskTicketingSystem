using System.ComponentModel.DataAnnotations;

namespace HackathonTaskTicketingSystem.Features.Epics;

public sealed record CreateEpicRequest(
    [Required] Guid TeamId,
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description);

public sealed record UpdateEpicRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description);

public sealed record EpicResponse(
    Guid Id,
    Guid TeamId,
    string Title,
    string? Description,
    DateTime CreatedAt,
    DateTime ModifiedAt);

public enum CreateEpicOutcome
{
    Success,
    EmptyTitle,
    TeamNotFound,
}

public enum UpdateEpicOutcome
{
    Success,
    EmptyTitle,
    NotFound,
}

public enum DeleteEpicOutcome
{
    Success,
    NotFound,
    // Blocked (409) is added in phase 4 once tickets can reference an epic.
}
