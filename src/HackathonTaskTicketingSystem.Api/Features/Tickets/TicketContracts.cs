using System.ComponentModel.DataAnnotations;
using HackathonTaskTicketingSystem.Domain.Enums;

namespace HackathonTaskTicketingSystem.Features.Tickets;

public sealed record CreateTicketRequest(
    [Required] Guid TeamId,
    Guid? EpicId,
    [Required] TicketType Type,
    TicketState? State,
    [Required, MaxLength(500)] string Title,
    [Required] string Body);

public sealed record UpdateTicketRequest(
    [Required] Guid TeamId,
    Guid? EpicId,
    [Required] TicketType Type,
    [Required] TicketState State,
    [Required, MaxLength(500)] string Title,
    [Required] string Body);

public sealed record CreatedByDto(Guid Id, string Email);

public sealed record TicketResponse(
    Guid Id,
    Guid TeamId,
    Guid? EpicId,
    TicketType Type,
    TicketState State,
    string Title,
    string Body,
    CreatedByDto CreatedBy,
    DateTime CreatedAt,
    DateTime ModifiedAt);

public enum TicketWriteOutcome
{
    Success,
    EmptyTitle,
    EmptyBody,
    TeamNotFound,
    EpicInvalid,
    NotFound,
}
