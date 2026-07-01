using HackathonTaskTicketingSystem.Domain.Entities;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Features.Epics;

/// <summary>
/// Epic CRUD. The owning team is set once at creation and never changed here.
/// </summary>
public sealed class EpicService
{
    private readonly AppDbContext _dbContext;

    public EpicService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EpicResponse>> ListAsync(Guid? teamId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Epics.AsQueryable();
        if (teamId is { } id)
        {
            query = query.Where(e => e.TeamId == id);
        }

        return await query
            .OrderBy(e => e.Title)
            .Select(e => new EpicResponse(e.Id, e.TeamId, e.Title, e.Description, e.CreatedAt, e.ModifiedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Epic?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Epics.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(CreateEpicOutcome Outcome, Epic? Epic)> CreateAsync(
        Guid teamId, string title, string? description, CancellationToken cancellationToken)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            return (CreateEpicOutcome.EmptyTitle, null);
        }

        if (!await _dbContext.Teams.AnyAsync(t => t.Id == teamId, cancellationToken))
        {
            return (CreateEpicOutcome.TeamNotFound, null);
        }

        var epic = new Epic
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Title = trimmedTitle,
            Description = NormalizeDescription(description),
        };
        _dbContext.Epics.Add(epic);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (CreateEpicOutcome.Success, epic);
    }

    public async Task<(UpdateEpicOutcome Outcome, Epic? Epic)> UpdateAsync(
        Guid id, string title, string? description, CancellationToken cancellationToken)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            return (UpdateEpicOutcome.EmptyTitle, null);
        }

        var epic = await _dbContext.Epics.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (epic is null)
        {
            return (UpdateEpicOutcome.NotFound, null);
        }

        // Team is intentionally not updated: an epic cannot move between teams.
        epic.Title = trimmedTitle;
        epic.Description = NormalizeDescription(description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (UpdateEpicOutcome.Success, epic);
    }

    public async Task<DeleteEpicOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var epic = await _dbContext.Epics.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (epic is null)
        {
            return DeleteEpicOutcome.NotFound;
        }

        if (await _dbContext.Tickets.AnyAsync(t => t.EpicId == id, cancellationToken))
        {
            return DeleteEpicOutcome.Blocked;
        }

        _dbContext.Epics.Remove(epic);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteEpicOutcome.Success;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }
}
