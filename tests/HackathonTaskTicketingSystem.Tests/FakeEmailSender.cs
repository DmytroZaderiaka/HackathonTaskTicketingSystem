using System.Text.RegularExpressions;
using HackathonTaskTicketingSystem.Common.Abstractions;

namespace HackathonTaskTicketingSystem.Tests;

/// <summary>
/// Test double for <see cref="IEmailSender"/> that captures sent messages instead of
/// contacting an SMTP server, and can extract the verification token from the last email.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Messages { get; } = new();

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        Messages.Add((toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }

    public string ExtractLatestToken()
    {
        var body = Messages[^1].Body;
        var match = Regex.Match(body, @"token=([^""&]+)");
        if (!match.Success)
        {
            throw new InvalidOperationException("No verification token found in the last email.");
        }

        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
