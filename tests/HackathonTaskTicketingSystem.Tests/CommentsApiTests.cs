using System.Net;
using System.Net.Http.Json;

namespace HackathonTaskTicketingSystem.Tests;

public sealed class CommentsApiTests
{
    private sealed record TeamDto(Guid Id);
    private sealed record TicketDto(Guid Id, DateTime ModifiedAt);
    private sealed record CommentDto(Guid Id, string Body, DateTime CreatedAt);

    private static async Task<Guid> CreateTeamAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/teams", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TeamDto>())!.Id;
    }

    private static async Task<TicketDto> CreateTicketAsync(HttpClient client, Guid teamId)
    {
        var response = await client.PostAsJsonAsync(
            "/tickets",
            new { teamId, epicId = (Guid?)null, type = "bug", state = "new", title = "T", body = "B" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>())!;
    }

    [Fact]
    public async Task Add_comment_returns_201_and_appears_in_the_list()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var ticket = await CreateTicketAsync(client, teamId);

        var add = await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "First comment" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var comments = await client.GetFromJsonAsync<List<CommentDto>>($"/tickets/{ticket.Id}/comments");
        Assert.Contains(comments!, c => c.Body == "First comment");
    }

    [Fact]
    public async Task Comments_are_returned_oldest_first()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var ticket = await CreateTicketAsync(client, teamId);

        await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "One" });
        await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "Two" });

        var comments = await client.GetFromJsonAsync<List<CommentDto>>($"/tickets/{ticket.Id}/comments");
        Assert.Equal(2, comments!.Count);
        Assert.True(comments[0].CreatedAt <= comments[1].CreatedAt);
    }

    [Fact]
    public async Task Add_comment_with_blank_body_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var ticket = await CreateTicketAsync(client, teamId);

        var add = await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task Add_comment_to_unknown_ticket_returns_404()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var add = await client.PostAsJsonAsync($"/tickets/{Guid.NewGuid()}/comments", new { body = "Hello" });
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
    }

    [Fact]
    public async Task Adding_a_comment_does_not_change_the_ticket_modified_at()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var ticket = await CreateTicketAsync(client, teamId);

        await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "Does not bump" });

        var reloaded = await client.GetFromJsonAsync<TicketDto>($"/tickets/{ticket.Id}");
        Assert.Equal(ticket.ModifiedAt, reloaded!.ModifiedAt);
    }

    [Fact]
    public async Task Deleting_a_ticket_with_a_comment_succeeds_and_cascades()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var ticket = await CreateTicketAsync(client, teamId);
        await client.PostAsJsonAsync($"/tickets/{ticket.Id}/comments", new { body = "To be removed with the ticket" });

        // Without cascade the ticket's FK from comments would block deletion.
        var delete = await client.DeleteAsync($"/tickets/{ticket.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
