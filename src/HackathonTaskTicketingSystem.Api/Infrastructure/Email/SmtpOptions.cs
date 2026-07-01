namespace HackathonTaskTicketingSystem.Infrastructure.Email;

/// <summary>
/// SMTP configuration, bound from the "SMTP" configuration section. Dev/demo points
/// at the MailPit container; production points at the real relay (relay1.dataart.com).
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "SMTP";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string FromAddress { get; set; } = "no-reply@ticketing.local";

    public string FromName { get; set; } = "Ticketing System";

    /// <summary>Optional SMTP auth username. Empty for MailPit.</summary>
    public string? Username { get; set; }

    /// <summary>Optional SMTP auth password. Empty for MailPit.</summary>
    public string? Password { get; set; }
}
