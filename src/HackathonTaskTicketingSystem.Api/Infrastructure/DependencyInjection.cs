using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Common.Configuration;
using HackathonTaskTicketingSystem.Infrastructure.Auth;
using HackathonTaskTicketingSystem.Infrastructure.Email;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using HackathonTaskTicketingSystem.Infrastructure.Persistence.Interceptors;
using HackathonTaskTicketingSystem.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HackathonTaskTicketingSystem.Infrastructure;

/// <summary>
/// Registers persistence and cross-cutting infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName));
        services.AddOptions<AppOptions>().Bind(configuration.GetSection(AppOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
