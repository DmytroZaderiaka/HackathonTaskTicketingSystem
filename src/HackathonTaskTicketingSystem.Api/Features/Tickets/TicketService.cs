using System.Linq.Expressions;
using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Domain.Entities;
using HackathonTaskTicketingSystem.Domain.Enums;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Features.Tickets;

/// <summary>
/// Ticket CRUD with server-side enforcement of enum values, required fields, and the
/// "epic must belong to the ticket's team" rule.
/// </summary>
public sealed class TicketService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public TicketService(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TicketResponse>> ListAsync(
        Guid teamId, TicketType? type, Guid? epicId, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets.Where(t => t.TeamId == teamId);

        if (type is { } ticketType)
        {
            query = query.Where(t => t.Type == ticketType);
        }

        if (epicId is { } epic)
        {
            query = query.Where(t => t.EpicId == epic);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(t => t.ModifiedAt)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .Where(t => t.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(TicketWriteOutcome Outcome, Guid Id)> CreateAsync(
        CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        if (title.Length == 0)
        {
            return (TicketWriteOutcome.EmptyTitle, Guid.Empty);
        }

        if (body.Length == 0)
        {
            return (TicketWriteOutcome.EmptyBody, Guid.Empty);
        }

        if (!await _dbContext.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken))
        {
            return (TicketWriteOutcome.TeamNotFound, Guid.Empty);
        }

        if (!await IsEpicValidAsync(request.EpicId, request.TeamId, cancellationToken))
        {
            return (TicketWriteOutcome.EpicInvalid, Guid.Empty);
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            EpicId = request.EpicId,
            Type = request.Type,
            State = request.State ?? TicketState.New,
            Title = title,
            Body = body,
            CreatedById = _currentUser.UserId!.Value,
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (TicketWriteOutcome.Success, ticket.Id);
    }

    public async Task<TicketWriteOutcome> UpdateAsync(
        Guid id, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        if (title.Length == 0)
        {
            return TicketWriteOutcome.EmptyTitle;
        }

        if (body.Length == 0)
        {
            return TicketWriteOutcome.EmptyBody;
        }

        var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return TicketWriteOutcome.NotFound;
        }

        if (!await _dbContext.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken))
        {
            return TicketWriteOutcome.TeamNotFound;
        }

        // When the team changes, the epic must belong to the new team (or be cleared).
        if (!await IsEpicValidAsync(request.EpicId, request.TeamId, cancellationToken))
        {
            return TicketWriteOutcome.EpicInvalid;
        }

        ticket.TeamId = request.TeamId;
        ticket.EpicId = request.EpicId;
        ticket.Type = request.Type;
        ticket.State = request.State;
        ticket.Title = title;
        ticket.Body = body;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return TicketWriteOutcome.Success;
    }

    public async Task<bool> ChangeStateAsync(Guid id, TicketState state, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        // Any state -> any state is allowed. If unchanged, EF tracks no change and
        // ModifiedAt is not advanced (dropping a card back in its own column).
        ticket.State = state;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        // Comments are removed together with the ticket (cascade configured in phase 5).
        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> IsEpicValidAsync(Guid? epicId, Guid teamId, CancellationToken cancellationToken)
    {
        if (epicId is not { } id)
        {
            return true; // No epic is always valid.
        }

        return await _dbContext.Epics.AnyAsync(e => e.Id == id && e.TeamId == teamId, cancellationToken);
    }

    private static readonly Expression<Func<Ticket, TicketResponse>> Projection = t => new TicketResponse(
        t.Id,
        t.TeamId,
        t.EpicId,
        t.Type,
        t.State,
        t.Title,
        t.Body,
        new CreatedByDto(t.CreatedBy.Id, t.CreatedBy.Email),
        t.CreatedAt,
        t.ModifiedAt);
}
