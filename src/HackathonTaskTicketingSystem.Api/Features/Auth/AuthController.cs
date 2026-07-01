using System.Security.Claims;
using HackathonTaskTicketingSystem.Common.Abstractions;
using HackathonTaskTicketingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackathonTaskTicketingSystem.Features.Auth;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(AuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> Signup(SignupRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _authService.SignupAsync(request.Email, request.Password, cancellationToken);
        return outcome switch
        {
            SignupOutcome.Success => StatusCode(StatusCodes.Status201Created),
            SignupOutcome.EmailAlreadyRegistered => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already registered",
                detail: "An account with this email address already exists."),
            _ => throw new InvalidOperationException($"Unhandled signup outcome: {outcome}"),
        };
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing verification token");
        }

        var outcome = await _authService.VerifyEmailAsync(token, cancellationToken);
        return outcome switch
        {
            VerifyEmailOutcome.Success => Ok(new { message = "Email verified. You can now log in." }),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid or expired verification token"),
        };
    }

    [AllowAnonymous]
    [HttpPost("resend")]
    public async Task<IActionResult> Resend(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResendVerificationAsync(request.Email, cancellationToken);

        // Always report success so the endpoint cannot reveal whether an account exists.
        return Ok(new { message = "If an unverified account exists for that email, a new verification link has been sent." });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var (outcome, user) = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
        switch (outcome)
        {
            case LoginOutcome.Success:
                await SignInAsync(user!);
                return Ok(new CurrentUserResponse(user!.Id, user.Email));

            case LoginOutcome.EmailNotVerified:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Email not verified",
                    detail: "Please verify your email address before logging in.");

            default:
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid credentials");
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(userId, _currentUser.Email ?? string.Empty));
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
