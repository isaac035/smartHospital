namespace SmartHospital.Api.Configuration;

/// <summary>
/// Connection to the internal Agent 1 (Clinical Triage + Doctor Matching) Python service.
/// Configure via environment variables, never committed files:
///   Agent1__BaseUrl, Agent1__InternalApiKey, Agent1__TimeoutSeconds,
///   Agent1__RateLimitPermits, Agent1__RateLimitWindowMinutes
/// </summary>
public class Agent1Settings
{
    public const string SectionName = "Agent1";

    public string BaseUrl { get; set; } = "http://localhost:8001";

    /// <summary>Shared secret sent as X-Internal-Api-Key. Empty = Agent 1 disabled.</summary>
    public string InternalApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 25;

    /// <summary>Triage requests allowed per patient per window (controls LLM cost/abuse).</summary>
    public int RateLimitPermits { get; set; } = 10;

    public int RateLimitWindowMinutes { get; set; } = 10;
}
