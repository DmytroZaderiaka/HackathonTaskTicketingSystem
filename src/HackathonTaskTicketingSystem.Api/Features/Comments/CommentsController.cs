using Microsoft.AspNetCore.Mvc;

namespace HackathonTaskTicketingSystem.Features.Comments;

[ApiController]
[Route("tickets/{ticketId:guid}/comments")]
public sealed class CommentsController : ControllerBase
{
    private readonly CommentService _commentService;

    public CommentsController(CommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid ticketId, CancellationToken cancellationToken)
    {
        var comments = await _commentService.ListAsync(ticketId, cancellationToken);
        return comments is null ? TicketNotFoundProblem() : Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid ticketId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var (outcome, comment) = await _commentService.AddAsync(ticketId, request.Body, cancellationToken);
        return outcome switch
        {
            AddCommentOutcome.Success => CreatedAtAction(nameof(List), new { ticketId }, comment),
            AddCommentOutcome.EmptyBody => Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Comment body is required"),
            AddCommentOutcome.TicketNotFound => TicketNotFoundProblem(),
            _ => throw new InvalidOperationException($"Unhandled add-comment outcome: {outcome}"),
        };
    }

    private IActionResult TicketNotFoundProblem() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Ticket not found");
}
