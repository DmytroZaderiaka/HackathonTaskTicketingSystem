using System.ComponentModel.DataAnnotations;

namespace HackathonTaskTicketingSystem.Features.Comments;

public sealed record CreateCommentRequest([Required] string Body);

public sealed record CommentAuthorDto(Guid Id, string Email);

public sealed record CommentResponse(
    Guid Id,
    Guid TicketId,
    CommentAuthorDto Author,
    string Body,
    DateTime CreatedAt);

public enum AddCommentOutcome
{
    Success,
    TicketNotFound,
    EmptyBody,
}
