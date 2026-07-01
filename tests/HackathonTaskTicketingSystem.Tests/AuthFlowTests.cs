using System.Net;
using System.Net.Http.Json;

namespace HackathonTaskTicketingSystem.Tests;

public sealed class AuthFlowTests
{
    private sealed record Credentials(string Email, string Password);

    [Fact]
    public async Task Signup_verify_then_login_succeeds_and_login_is_blocked_until_verified()
    {
        await using var factory = new AuthTestFactory();
        var client = factory.CreateClient();
        var credentials = new Credentials("user@example.com", "password123");

        // Sign up.
        var signup = await client.PostAsJsonAsync("/auth/signup", credentials);
        Assert.Equal(HttpStatusCode.Created, signup.StatusCode);

        // Login is blocked before the email is verified.
        var earlyLogin = await client.PostAsJsonAsync("/auth/login", credentials);
        Assert.Equal(HttpStatusCode.Forbidden, earlyLogin.StatusCode);

        // Verify using the token captured from the outgoing email.
        var token = factory.Email.ExtractLatestToken();
        var verify = await client.GetAsync($"/auth/verify-email?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        // Login now succeeds and issues the auth cookie.
        var login = await client.PostAsJsonAsync("/auth/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // The authenticated endpoint is reachable with the cookie.
        var me = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Me_without_authentication_returns_401()
    {
        await using var factory = new AuthTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_signup_returns_409()
    {
        await using var factory = new AuthTestFactory();
        var client = factory.CreateClient();
        var credentials = new Credentials("dup@example.com", "password123");

        var first = await client.PostAsJsonAsync("/auth/signup", credentials);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/auth/signup", credentials);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
