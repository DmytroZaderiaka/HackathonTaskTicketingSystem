using System.Net;
using System.Net.Http.Json;

namespace HackathonTaskTicketingSystem.Tests;

public sealed class TeamsApiTests
{
    private sealed record TeamDto(Guid Id, string Name, DateTime CreatedAt, DateTime ModifiedAt);

    [Fact]
    public async Task Create_then_list_returns_the_team()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/teams", new { name = "Backend Team" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var teams = await client.GetFromJsonAsync<List<TeamDto>>("/teams");
        Assert.NotNull(teams);
        Assert.Contains(teams!, t => t.Name == "Backend Team");
    }

    [Fact]
    public async Task Create_with_duplicate_name_is_case_insensitive_and_returns_409()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/teams", new { name = "Alpha" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/teams", new { name = "alpha" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Rename_updates_the_team()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/teams", new { name = "Original" });
        var team = await create.Content.ReadFromJsonAsync<TeamDto>();

        var rename = await client.PutAsJsonAsync($"/teams/{team!.Id}", new { name = "Renamed" });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        var updated = await rename.Content.ReadFromJsonAsync<TeamDto>();
        Assert.Equal("Renamed", updated!.Name);
    }

    [Fact]
    public async Task Delete_removes_the_team()
    {
        await using var factory = new ApiTestFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/teams", new { name = "Temporary" });
        var team = await create.Content.ReadFromJsonAsync<TeamDto>();

        var delete = await client.DeleteAsync($"/teams/{team!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync($"/teams/{team.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Creating_a_team_without_authentication_returns_401()
    {
        await using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/teams", new { name = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
