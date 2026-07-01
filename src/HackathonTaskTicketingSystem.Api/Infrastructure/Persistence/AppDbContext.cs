using HackathonTaskTicketingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Infrastructure.Persistence;

/// <summary>
/// Application database context. Entity sets and configurations are added per feature phase.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Epic> Epics => Set<Epic>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations live next to their entities and are picked up automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
