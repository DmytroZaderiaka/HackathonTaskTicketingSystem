using HackathonTaskTicketingSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HackathonTaskTicketingSystem.Features.Teams;

[ApiController]
[Route("teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly TeamService _teamService;

    public TeamsController(TeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    public async Task<IReadOnlyList<TeamResponse>> List(CancellationToken cancellationToken)
        => await _teamService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var teams = await _teamService.ListAsync(cancellationToken);
        var team = teams.FirstOrDefault(t => t.Id == id);
        return team is null ? NotFoundProblem() : Ok(team);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var (outcome, team) = await _teamService.CreateAsync(request.Name, cancellationToken);
        return outcome switch
        {
            CreateTeamOutcome.Success => CreatedAtAction(nameof(GetById), new { id = team!.Id }, ToResponse(team)),
            CreateTeamOutcome.EmptyName => EmptyNameProblem(),
            CreateTeamOutcome.NameConflict => NameConflictProblem(),
            _ => throw new InvalidOperationException($"Unhandled create outcome: {outcome}"),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var (outcome, team) = await _teamService.RenameAsync(id, request.Name, cancellationToken);
        return outcome switch
        {
            UpdateTeamOutcome.Success => Ok(ToResponse(team!)),
            UpdateTeamOutcome.EmptyName => EmptyNameProblem(),
            UpdateTeamOutcome.NotFound => NotFoundProblem(),
            UpdateTeamOutcome.NameConflict => NameConflictProblem(),
            _ => throw new InvalidOperationException($"Unhandled update outcome: {outcome}"),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await _teamService.DeleteAsync(id, cancellationToken);
        return outcome switch
        {
            DeleteTeamOutcome.Success => NoContent(),
            DeleteTeamOutcome.NotFound => NotFoundProblem(),
            DeleteTeamOutcome.Blocked => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Team is not empty",
                detail: "Delete its epics and tickets before deleting the team."),
            _ => throw new InvalidOperationException($"Unhandled delete outcome: {outcome}"),
        };
    }

    private static TeamResponse ToResponse(Team team) =>
        new(team.Id, team.Name, team.CreatedAt, team.ModifiedAt);

    private IActionResult EmptyNameProblem() =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Team name is required");

    private IActionResult NameConflictProblem() =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "A team with this name already exists");

    private IActionResult NotFoundProblem() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Team not found");
}
