using HackathonTaskTicketingSystem.Common.ErrorHandling;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Services ---
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // RFC 7807 problem details for all error responses.
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        // Persistence. Connection string comes from configuration (env var
        // ConnectionStrings__Default in Docker Compose).
        var connectionString = builder.Configuration.GetConnectionString("Default");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // --- HTTP pipeline ---
        // TLS terminates at the reverse proxy (nginx), so no HTTPS redirection here.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health"); // Public readiness/liveness endpoint.

        await ApplyMigrationsAsync(app);

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
