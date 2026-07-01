using System.Net;
using System.Net.Http.Json;

namespace HackathonTaskTicketingSystem.Tests;

public sealed class EpicsApiTests
{
    private sealed record TeamDto(Guid Id, string Name);
    private sealed record EpicDto(Guid Id, Guid TeamId, string Title, string? Description);

    private static async Task<Guid> CreateTeamAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/teams", new { name });
        response.EnsureSuccessStatusCode();
        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        return team!.Id;
    }

    [Fact]
    public async Task Create_epic_returns_201_and_lists_under_its_team()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team A");

        var create = await client.PostAsJsonAsync("/epics", new { teamId, title = "Epic 1", description = "Desc" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var epics = await client.GetFromJsonAsync<List<EpicDto>>($"/epics?teamId={teamId}");
        Assert.NotNull(epics);
        Assert.Contains(epics!, e => e.Title == "Epic 1");
    }

    [Fact]
    public async Task Create_epic_with_blank_title_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team B");

        var create = await client.PostAsJsonAsync("/epics", new { teamId, title = "   ", description = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Create_epic_for_unknown_team_returns_400()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync(
            "/epics", new { teamId = Guid.NewGuid(), title = "Orphan", description = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Update_epic_changes_title_and_description()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team C");
        var create = await client.PostAsJsonAsync("/epics", new { teamId, title = "Before", description = (string?)null });
        var epic = await create.Content.ReadFromJsonAsync<EpicDto>();

        var update = await client.PutAsJsonAsync($"/epics/{epic!.Id}", new { title = "After", description = "Now set" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updated = await update.Content.ReadFromJsonAsync<EpicDto>();
        Assert.Equal("After", updated!.Title);
        Assert.Equal("Now set", updated.Description);
    }

    [Fact]
    public async Task Delete_epic_removes_it()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team D");
        var create = await client.PostAsJsonAsync("/epics", new { teamId, title = "Temp", description = (string?)null });
        var epic = await create.Content.ReadFromJsonAsync<EpicDto>();

        var delete = await client.DeleteAsync($"/epics/{epic!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync($"/epics/{epic.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_team_that_still_has_an_epic_returns_409()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client, "Team E");
        await client.PostAsJsonAsync("/epics", new { teamId, title = "Blocking epic", description = (string?)null });

        var deleteTeam = await client.DeleteAsync($"/teams/{teamId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteTeam.StatusCode);
    }
}
