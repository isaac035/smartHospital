namespace SmartHospital.Api.Configuration;

/// <summary>Connection to the internal Agent 2 Python service.</summary>
public class Agent2Settings
{
    public const string SectionName = "Agent2";
    public string BaseUrl { get; set; } = "http://localhost:8002";
    public string InternalApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
    public int RateLimitPermits { get; set; } = 10;
    public int RateLimitWindowMinutes { get; set; } = 10;
}
