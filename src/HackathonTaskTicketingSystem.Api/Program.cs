using System.Text.Json;
using System.Text.Json.Serialization;
using HackathonTaskTicketingSystem.Common.ErrorHandling;
using HackathonTaskTicketingSystem.Features.Auth;
using HackathonTaskTicketingSystem.Features.Comments;
using HackathonTaskTicketingSystem.Features.Epics;
using HackathonTaskTicketingSystem.Features.Teams;
using HackathonTaskTicketingSystem.Features.Tickets;
using HackathonTaskTicketingSystem.Infrastructure;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Services ---
        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
                // Serialize enums as canonical snake_case values (e.g. ready_for_implementation).
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
        builder.Services.AddOpenApi();

        // RFC 7807 problem details for all error responses.
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<TeamService>();
        builder.Services.AddScoped<EpicService>();
        builder.Services.AddScoped<TicketService>();
        builder.Services.AddScoped<CommentService>();
        builder.Services.AddHealthChecks();

        // Cookie-based authentication. Session identifiers never appear in URLs.
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // Return status codes instead of HTML redirects for an API.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        // Every endpoint requires authentication unless it opts out with [AllowAnonymous].
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        var app = builder.Build();

        // --- HTTP pipeline ---
        // TLS terminates at the reverse proxy (nginx), so no HTTPS redirection here.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous(); // Public readiness/liveness endpoint.

        // Disabled in tests, which create the schema against an in-memory provider.
        if (app.Configuration.GetValue("RunMigrationsOnStartup", true))
        {
            await ApplyMigrationsAsync(app);
        }

        await app.RunAsync();
    }

    /// <summary>
    /// Applies pending EF Core migrations at startup so a fresh database is brought
    /// up to the current schema automatically (schema + migration metadata only).
    /// </summary>
    private static async Task ApplyMigrationsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
