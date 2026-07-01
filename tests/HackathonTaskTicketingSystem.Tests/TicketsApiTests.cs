using System.Net;
using System.Net.Http.Json;

namespace HackathonTaskTicketingSystem.Tests;

public sealed class TicketsApiTests
{
    private sealed record TeamDto(Guid Id);
    private sealed record EpicDto(Guid Id);
    private sealed record TicketDto(Guid Id, Guid TeamId, Guid? EpicId, string Type, string State, string Title, string Body);

    private static async Task<Guid> CreateTeamAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/teams", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TeamDto>())!.Id;
    }

    private static async Task<Guid> CreateEpicAsync(HttpClient client, Guid teamId, string title)
    {
        var response = await client.PostAsJsonAsync("/epics", new { teamId, title, description = (string?)null });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EpicDto>())!.Id;
    }

    private static object NewTicket(Guid teamId, Guid? epicId = null, string type = "bug", string state = "new",
        string title = "Ticket title", string body = "Ticket body")
        => new { teamId, epicId, type, state, title, body };

    [Fact]
    public async Task Create_ticket_returns_201_and_defaults_created_by_to_current_user()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync("author@example.com");
        var teamId = await CreateTeamAsync(client, "Team");

        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamId, title: "First"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var tickets = await client.GetFromJsonAsync<List<TicketDto>>($"/tickets?teamId={teamId}");
        Assert.Contains(tickets!, t => t.Title == "First");
    }

    [Fact]
    public async Task Create_ticket_with_epic_from_another_team_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamA = await CreateTeamAsync(client, "Team A");
        var teamB = await CreateTeamAsync(client, "Team B");
        var epicB = await CreateEpicAsync(client, teamB, "Epic in B");

        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamA, epicId: epicB));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Create_ticket_with_blank_title_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");

        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamId, title: "   "));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Create_ticket_with_invalid_state_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");

        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamId, state: "not_a_real_state"));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Update_ticket_changes_fields()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamId, title: "Before"));
        var ticket = await create.Content.ReadFromJsonAsync<TicketDto>();

        var update = await client.PutAsJsonAsync(
            $"/tickets/{ticket!.Id}",
            NewTicket(teamId, type: "fix", state: "in_progress", title: "After", body: "Updated"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updated = await update.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal("After", updated!.Title);
        Assert.Equal("in_progress", updated.State);
        Assert.Equal("fix", updated.Type);
    }

    [Fact]
    public async Task Delete_ticket_removes_it()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var create = await client.PostAsJsonAsync("/tickets", NewTicket(teamId));
        var ticket = await create.Content.ReadFromJsonAsync<TicketDto>();

        var delete = await client.DeleteAsync($"/tickets/{ticket!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync($"/tickets/{ticket.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_type()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        await client.PostAsJsonAsync("/tickets", NewTicket(teamId, type: "bug", title: "A bug"));
        await client.PostAsJsonAsync("/tickets", NewTicket(teamId, type: "feature", title: "A feature"));

        var bugs = await client.GetFromJsonAsync<List<TicketDto>>($"/tickets?teamId={teamId}&type=bug");
        Assert.All(bugs!, t => Assert.Equal("bug", t.Type));
        Assert.Contains(bugs!, t => t.Title == "A bug");
        Assert.DoesNotContain(bugs!, t => t.Title == "A feature");
    }

    [Fact]
    public async Task Deleting_an_epic_referenced_by_a_ticket_returns_409()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        var epicId = await CreateEpicAsync(client, teamId, "Epic");
        await client.PostAsJsonAsync("/tickets", NewTicket(teamId, epicId: epicId));

        var deleteEpic = await client.DeleteAsync($"/epics/{epicId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteEpic.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_team_with_a_ticket_returns_409()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team");
        await client.PostAsJsonAsync("/tickets", NewTicket(teamId));

        var deleteTeam = await client.DeleteAsync($"/teams/{teamId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteTeam.StatusCode);
    }
}
