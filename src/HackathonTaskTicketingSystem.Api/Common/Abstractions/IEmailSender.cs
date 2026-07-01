namespace HackathonTaskTicketingSystem.Common.Abstractions;

/// <summary>
/// Sends transactional email through the configured SMTP relay.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
