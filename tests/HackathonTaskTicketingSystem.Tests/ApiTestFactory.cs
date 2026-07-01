using System.Net.Http.Json;
using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using HackathonTaskTicketingSystem.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HackathonTaskTicketingSystem.Tests;

/// <summary>
/// Spins up the real application for integration tests, backed by a shared in-memory
/// SQLite database and a capturing email sender, so no Docker/Postgres/SMTP is required.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RunMigrationsOnStartup"] = "false",
                ["App:BaseUrl"] = "http://localhost",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove every registration tied to the Npgsql AppDbContext, including the
            // options-configuration entries (EF Core 9+) that would otherwise apply
            // UseNpgsql alongside UseSqlite and register two providers. Matching on the
            // AppDbContext generic argument catches DbContextOptions<AppDbContext> and
            // IDbContextOptionsConfiguration<AppDbContext> without referencing the latter.
            var toRemove = services.Where(descriptor =>
                    descriptor.ServiceType == typeof(AppDbContext)
                    || descriptor.ServiceType == typeof(DbContextOptions)
                    || (descriptor.ServiceType.IsGenericType
                        && descriptor.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Replace with a shared in-memory SQLite context, keeping the auditing
            // interceptor so IAuditableEntity timestamps are still populated.
            _connection.Open();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options
                    .UseSqlite(_connection)
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

            // Capture outgoing email instead of sending it.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        return host;
    }

    /// <summary>
    /// Creates a client whose cookie belongs to a freshly registered, verified, logged-in
    /// user — the starting point for testing endpoints that require authentication.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "member@example.com", string password = "password123")
    {
        var client = CreateClient();

        var signup = await client.PostAsJsonAsync("/auth/signup", new { email, password });
        signup.EnsureSuccessStatusCode();

        var token = Email.ExtractLatestToken();
        var verify = await client.GetAsync($"/auth/verify-email?token={Uri.EscapeDataString(token)}");
        verify.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
