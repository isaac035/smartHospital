namespace SmartHospital.Api.Configuration;

public class Agent4Settings
{
    public const string SectionName = "Agent4";
    public string BaseUrl { get; set; } = "http://localhost:8004";
    public string InternalApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
