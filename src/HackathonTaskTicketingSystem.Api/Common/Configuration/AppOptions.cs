namespace HackathonTaskTicketingSystem.Common.Configuration;

/// <summary>
/// Application-level settings, bound from the "App" configuration section.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Public base URL used to build links in outgoing email (e.g. the
    /// email-verification link). In Docker this points at the backend API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5083";
}
