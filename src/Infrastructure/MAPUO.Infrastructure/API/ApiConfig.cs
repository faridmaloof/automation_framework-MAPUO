namespace MAPUO.Infrastructure.API;

/// <summary>
/// Configuración para pruebas de API.
/// </summary>
public class ApiConfig
{
    public string BaseUrl { get; set; } = "https://httpbin.org";
    public int TimeoutMs { get; set; } = 15000;
    public string AuthType { get; set; } = "None"; // None, Bearer, Basic
    public string? BearerToken { get; set; }
    public string? BasicUser { get; set; }
    public string? BasicPassword { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public bool GenerateReport { get; set; } = true;
    public List<string> Tags { get; set; } = new();
    public string EvidenceBasePath { get; set; } = "TestResults/Evidence";
}
