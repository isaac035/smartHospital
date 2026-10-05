namespace SmartHospital.Api.Configuration;

public class Agent3Settings
{
    public const string SectionName = "Agent3";
    public string BaseUrl { get; set; } = "http://localhost:8003";
    public string InternalApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
