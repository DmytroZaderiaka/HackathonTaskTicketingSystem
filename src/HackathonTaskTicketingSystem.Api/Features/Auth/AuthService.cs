using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Common.Configuration;
using HackathonTaskTicketingSystem.Domain.Entities;
using HackathonTaskTicketingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HackathonTaskTicketingSystem.Features.Auth;

/// <summary>
/// Authentication business logic: sign-up, email verification, resend, and credential
/// checks. Cookie sign-in/out itself is performed by the controller.
/// </summary>
public sealed class AuthService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly AppOptions _appOptions;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IClock clock,
        IOptions<AppOptions> appOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _clock = clock;
        _appOptions = appOptions.Value;
    }

    public async Task<SignupOutcome> SignupAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return SignupOutcome.EmailAlreadyRegistered;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(password),
            IsEmailVerified = false,
            CreatedAt = _clock.UtcNow,
        };
        _dbContext.Users.Add(user);

        var rawToken = IssueToken(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SendVerificationEmailAsync(user.Email, rawToken, cancellationToken);
        return SignupOutcome.Success;
    }

    public async Task<VerifyEmailOutcome> VerifyEmailAsync(string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(rawToken);
        var token = await _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= _clock.UtcNow)
        {
            return VerifyEmailOutcome.InvalidOrExpired;
        }

        token.UsedAt = _clock.UtcNow;
        token.User.IsEmailVerified = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return VerifyEmailOutcome.Success;
    }

    /// <summary>
    /// Re-issues a verification email. Silently no-ops for unknown or already-verified
    /// accounts so the endpoint cannot be used to enumerate registered emails.
    /// </summary>
    public async Task ResendVerificationAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || user.IsEmailVerified)
        {
            return;
        }

        // Issuing a new token invalidates earlier unused ones.
        var invalidatedAt = _clock.UtcNow;
        await _dbContext.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ForEachAsync(t => t.UsedAt = invalidatedAt, cancellationToken);

        var rawToken = IssueToken(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SendVerificationEmailAsync(user.Email, rawToken, cancellationToken);
    }

    public async Task<(LoginOutcome Outcome, User? User)> LoginAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return (LoginOutcome.InvalidCredentials, null);
        }

        if (!user.IsEmailVerified)
        {
            return (LoginOutcome.EmailNotVerified, null);
        }

        return (LoginOutcome.Success, user);
    }

    private string IssueToken(User user)
    {
        var rawToken = GenerateRawToken();
        _dbContext.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = _clock.UtcNow.Add(TokenLifetime),
            CreatedAt = _clock.UtcNow,
        });
        return rawToken;
    }

    private async Task SendVerificationEmailAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        var link = $"{_appOptions.BaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        var body = $"""
            <p>Welcome to the Ticketing System.</p>
            <p>Please verify your email address by clicking the link below. It is valid for 24 hours:</p>
            <p><a href="{link}">Verify my email</a></p>
            """;

        await _emailSender.SendAsync(email, "Verify your email", body, cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateRawToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
