using System.ComponentModel.DataAnnotations;

namespace HackathonTaskTicketingSystem.Features.Auth;

public sealed record SignupRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record ResendVerificationRequest(
    [Required, EmailAddress] string Email);

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
