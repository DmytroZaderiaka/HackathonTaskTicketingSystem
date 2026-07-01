using HackathonTaskTicketingSystem.Domain.Entities;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Features.Teams;

/// <summary>
/// Team CRUD with case-insensitive unique names. Timestamps are maintained by the
/// auditing interceptor, so this service never sets CreatedAt/ModifiedAt directly.
/// </summary>
public sealed class TeamService
{
    private readonly AppDbContext _dbContext;

    public TeamService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TeamResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse(t.Id, t.Name, t.CreatedAt, t.ModifiedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<(CreateTeamOutcome Outcome, Team? Team)> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            return (CreateTeamOutcome.EmptyName, null);
        }

        var normalizedName = Normalize(trimmedName);
        if (await _dbContext.Teams.AnyAsync(t => t.NormalizedName == normalizedName, cancellationToken))
        {
            return (CreateTeamOutcome.NameConflict, null);
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            NormalizedName = normalizedName,
        };
        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (CreateTeamOutcome.Success, team);
    }

    public async Task<(UpdateTeamOutcome Outcome, Team? Team)> RenameAsync(
        Guid id, string name, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            return (UpdateTeamOutcome.EmptyName, null);
        }

        var team = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (team is null)
        {
            return (UpdateTeamOutcome.NotFound, null);
        }

        var normalizedName = Normalize(trimmedName);
        var nameTaken = await _dbContext.Teams
            .AnyAsync(t => t.NormalizedName == normalizedName && t.Id != id, cancellationToken);
        if (nameTaken)
        {
            return (UpdateTeamOutcome.NameConflict, null);
        }

        // Assigning the same values is a no-op for EF change tracking, so ModifiedAt
        // is only advanced when the name actually changes.
        team.Name = trimmedName;
        team.NormalizedName = normalizedName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (UpdateTeamOutcome.Success, team);
    }

    public async Task<DeleteTeamOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (team is null)
        {
            return DeleteTeamOutcome.NotFound;
        }

        // A team cannot be deleted while it still holds epics (tickets are added in phase 4).
        if (await _dbContext.Epics.AnyAsync(e => e.TeamId == id, cancellationToken))
        {
            return DeleteTeamOutcome.Blocked;
        }

        _dbContext.Teams.Remove(team);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteTeamOutcome.Success;
    }

    private static string Normalize(string trimmedName) => trimmedName.ToUpperInvariant();
}
