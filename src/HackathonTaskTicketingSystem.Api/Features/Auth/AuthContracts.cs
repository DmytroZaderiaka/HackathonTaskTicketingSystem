using System.ComponentModel.DataAnnotations;

namespace HackathonTaskTicketingSystem.Features.Auth;

public sealed record SignupRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(8)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record ResendVerificationRequest(
    [property: Required, EmailAddress] string Email);

public sealed record CurrentUserResponse(Guid Id, string Email);

public enum SignupOutcome
{
    Success,
    EmailAlreadyRegistered,
}

public enum VerifyEmailOutcome
{
    Success,
    InvalidOrExpired,
}

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    EmailNotVerified,
}
