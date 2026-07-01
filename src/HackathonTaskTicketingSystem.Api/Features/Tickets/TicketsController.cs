using HackathonTaskTicketingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HackathonTaskTicketingSystem.Features.Tickets;

[ApiController]
[Route("tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;

    public TicketsController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IReadOnlyList<TicketResponse>> List(
        [FromQuery] Guid teamId,
        [FromQuery] TicketType? type,
        [FromQuery] Guid? epicId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => await _ticketService.ListAsync(teamId, type, epicId, search, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetAsync(id, cancellationToken);
        return ticket is null ? NotFoundProblem() : Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var (outcome, id) = await _ticketService.CreateAsync(request, cancellationToken);
        if (outcome != TicketWriteOutcome.Success)
        {
            return MapFailure(outcome);
        }

        var created = await _ticketService.GetAsync(id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _ticketService.UpdateAsync(id, request, cancellationToken);
        if (outcome != TicketWriteOutcome.Success)
        {
            return MapFailure(outcome);
        }

        var updated = await _ticketService.GetAsync(id, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/state")]
    public async Task<IActionResult> ChangeState(
        Guid id, ChangeTicketStateRequest request, CancellationToken cancellationToken)
    {
        var changed = await _ticketService.ChangeStateAsync(id, request.State, cancellationToken);
        if (!changed)
        {
            return NotFoundProblem();
        }

        var updated = await _ticketService.GetAsync(id, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ticketService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFoundProblem();
    }

    private IActionResult MapFailure(TicketWriteOutcome outcome) => outcome switch
    {
        TicketWriteOutcome.EmptyTitle => Problem(
            statusCode: StatusCodes.Status400BadRequest, title: "Ticket title is required"),
        TicketWriteOutcome.EmptyBody => Problem(
            statusCode: StatusCodes.Status400BadRequest, title: "Ticket body is required"),
        TicketWriteOutcome.TeamNotFound => Problem(
            statusCode: StatusCodes.Status400BadRequest, title: "Team not found"),
        TicketWriteOutcome.EpicInvalid => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid epic",
            detail: "The epic must belong to the ticket's team."),
        TicketWriteOutcome.NotFound => NotFoundProblem(),
        _ => throw new InvalidOperationException($"Unhandled ticket outcome: {outcome}"),
    };

    private IActionResult NotFoundProblem() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Ticket not found");
}
