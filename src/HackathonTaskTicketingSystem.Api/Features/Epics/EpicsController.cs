using HackathonTaskTicketingSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HackathonTaskTicketingSystem.Features.Epics;

[ApiController]
[Route("epics")]
public sealed class EpicsController : ControllerBase
{
    private readonly EpicService _epicService;

    public EpicsController(EpicService epicService)
    {
        _epicService = epicService;
    }

    [HttpGet]
    public async Task<IReadOnlyList<EpicResponse>> List([FromQuery] Guid? teamId, CancellationToken cancellationToken)
        => await _epicService.ListAsync(teamId, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var epic = await _epicService.FindAsync(id, cancellationToken);
        return epic is null ? NotFoundProblem() : Ok(ToResponse(epic));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEpicRequest request, CancellationToken cancellationToken)
    {
        var (outcome, epic) = await _epicService.CreateAsync(
            request.TeamId, request.Title, request.Description, cancellationToken);
        return outcome switch
        {
            CreateEpicOutcome.Success => CreatedAtAction(nameof(GetById), new { id = epic!.Id }, ToResponse(epic)),
            CreateEpicOutcome.EmptyTitle => EmptyTitleProblem(),
            CreateEpicOutcome.TeamNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Team not found"),
            _ => throw new InvalidOperationException($"Unhandled create outcome: {outcome}"),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEpicRequest request, CancellationToken cancellationToken)
    {
        var (outcome, epic) = await _epicService.UpdateAsync(id, request.Title, request.Description, cancellationToken);
        return outcome switch
        {
            UpdateEpicOutcome.Success => Ok(ToResponse(epic!)),
            UpdateEpicOutcome.EmptyTitle => EmptyTitleProblem(),
            UpdateEpicOutcome.NotFound => NotFoundProblem(),
            _ => throw new InvalidOperationException($"Unhandled update outcome: {outcome}"),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await _epicService.DeleteAsync(id, cancellationToken);
        return outcome switch
        {
            DeleteEpicOutcome.Success => NoContent(),
            DeleteEpicOutcome.NotFound => NotFoundProblem(),
            _ => throw new InvalidOperationException($"Unhandled delete outcome: {outcome}"),
        };
    }

    private static EpicResponse ToResponse(Epic epic) =>
        new(epic.Id, epic.TeamId, epic.Title, epic.Description, epic.CreatedAt, epic.ModifiedAt);

    private IActionResult EmptyTitleProblem() =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Epic title is required");

    private IActionResult NotFoundProblem() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Epic not found");
}
