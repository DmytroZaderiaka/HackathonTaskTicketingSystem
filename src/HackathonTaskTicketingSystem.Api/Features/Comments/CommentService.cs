using System.Linq.Expressions;
using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Domain.Entities;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Features.Comments;

/// <summary>
/// Ticket comments. Adding a comment never touches the ticket, so the ticket's
/// ModifiedAt (and therefore its board ordering) is unaffected.
/// </summary>
public sealed class CommentService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CommentService(AppDbContext dbContext, ICurrentUser currentUser, IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CommentResponse>?> ListAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Tickets.AnyAsync(t => t.Id == ticketId, cancellationToken))
        {
            return null; // ticket not found
        }

        return await _dbContext.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<(AddCommentOutcome Outcome, CommentResponse? Comment)> AddAsync(
        Guid ticketId, string body, CancellationToken cancellationToken)
    {
        var trimmedBody = body.Trim();
        if (trimmedBody.Length == 0)
        {
            return (AddCommentOutcome.EmptyBody, null);
        }

        if (!await _dbContext.Tickets.AnyAsync(t => t.Id == ticketId, cancellationToken))
        {
            return (AddCommentOutcome.TicketNotFound, null);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = _currentUser.UserId!.Value,
            Body = trimmedBody,
            CreatedAt = _clock.UtcNow,
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload with author details for the response.
        var response = await _dbContext.Comments
            .Where(c => c.Id == comment.Id)
            .Select(Projection)
            .FirstAsync(cancellationToken);

        return (AddCommentOutcome.Success, response);
    }

    private static readonly Expression<Func<Comment, CommentResponse>> Projection = c => new CommentResponse(
        c.Id,
        c.TicketId,
        new CommentAuthorDto(c.Author.Id, c.Author.Email),
        c.Body,
        c.CreatedAt);
}
