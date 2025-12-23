using System.Text.Json;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;
using MAPUO.Infrastructure.API;
using MAPUO.Infrastructure.DI;
using TechTalk.SpecFlow;
using System.Linq;
using NUnit.Framework;
using Allure.Net.Commons;

namespace MAPUO.StepDefinitions;

[Binding]
public class ApiHooks
{
    private static ApiConfig? _cachedConfig;
    private static readonly object _lock = new();
    private IServiceProvider? _serviceProvider;
    private IActor? _actor;
    private readonly FeatureContext _featureContext;
    private static readonly AllureLifecycle _allure = AllureLifecycle.Instance;

    public ApiHooks(FeatureContext featureContext)
    {
        _featureContext = featureContext;
    }

    [BeforeScenario("api")]
    public void BeforeApiScenario(ScenarioContext scenarioContext)
    {
        var apiConfig = LoadApiConfig();

        if (apiConfig.Tags.Any())
        {
            // Incluir tags de escenario y de característica para que los filtros funcionen en ambos niveles
            var rawTags = (scenarioContext.ScenarioInfo.Tags ?? Array.Empty<string>())
                .Concat(_featureContext.FeatureInfo.Tags ?? Array.Empty<string>());

            // Normalizar tags para evitar desajustes por mayúsculas o símbolos '@'
            var scenarioTags = rawTags
                .Select(t => t.Trim().TrimStart('@').ToLowerInvariant())
                .ToList();
            var required = apiConfig.Tags
                .Select(t => t.Trim().TrimStart('@').ToLowerInvariant())
                .ToList();

            if (!required.Any(scenarioTags.Contains))
            {
                Assert.Ignore($"Escenario API omitido por filtro de tags: {string.Join(',', apiConfig.Tags)}");
            }
        }
        _serviceProvider = ContainerBootstrapper.BuildApi(apiConfig);
        _actor = ContainerBootstrapper.CreateApiActor(_serviceProvider, "ApiUser");
        scenarioContext.Set(_actor, "Actor");
        scenarioContext.Set(apiConfig, "ApiConfig");
        Console.WriteLine($"\n=== Iniciando escenario API: {scenarioContext.ScenarioInfo.Title} ===");
        Console.WriteLine($"BaseUrl: {apiConfig.BaseUrl}, Timeout: {apiConfig.TimeoutMs}ms, Auth: {apiConfig.AuthType}");
    }

    [AfterScenario("api")]
    public void AfterApiScenario(ScenarioContext scenarioContext)
    {
        TryAttachEvidence(scenarioContext);

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        Console.WriteLine($"=== Fin escenario API: {scenarioContext.ScenarioInfo.Title} ===\n");
    }

    private void TryAttachEvidence(ScenarioContext scenarioContext)
    {
        var config = scenarioContext.TryGetValue("ApiConfig", out ApiConfig? cfgFromContext) ? cfgFromContext : _cachedConfig;
        if (config == null || !config.GenerateReport)
        {
            return;
        }

        if (!scenarioContext.TryGetValue("api_evidence_files", out List<string>? files) || files == null || files.Count == 0)
        {
            return;
        }

        foreach (var file in files)
        {
            if (!File.Exists(file)) continue;
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var type = ext switch
            {
                ".json" => "application/json",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
            var name = Path.GetFileName(file);
            AllureApi.AddAttachment(name, type, file);
        }
    }

    private static ApiConfig LoadApiConfig()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        lock (_lock)
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var config = new ApiConfig();
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apiconfig.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var loaded = JsonSerializer.Deserialize<ApiConfig>(json);
                    if (loaded != null)
                    {
                        config = loaded;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar apiconfig.json: {ex.Message}");
                }
            }

            // Overrides env
            config.BaseUrl = EnvOr(config.BaseUrl, "API_BASE_URL");
            config.TimeoutMs = IntEnvOr(config.TimeoutMs, "API_TIMEOUT_MS");
            config.AuthType = EnvOr(config.AuthType, "API_AUTH_TYPE");
            config.BearerToken = EnvOr(config.BearerToken, "API_BEARER_TOKEN");
            config.BasicUser = EnvOr(config.BasicUser, "API_BASIC_USER");
            config.BasicPassword = EnvOr(config.BasicPassword, "API_BASIC_PASSWORD");
            config.GenerateReport = BoolEnvOr(config.GenerateReport, "GENERATE_REPORT");
            config.EvidenceBasePath = EnvOr(config.EvidenceBasePath, "EVIDENCE_BASE_PATH");

            var tagsEnv = Environment.GetEnvironmentVariable("TEST_TAGS");
            if (!string.IsNullOrWhiteSpace(tagsEnv))
            {
                var list = tagsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (list.Count > 0)
                {
                    config.Tags = list;
                }
            }

            var headersEnv = Environment.GetEnvironmentVariable("API_DEFAULT_HEADERS");
            if (!string.IsNullOrWhiteSpace(headersEnv))
            {
                var dict = new Dictionary<string, string>();
                var pairs = headersEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in pairs)
                {
                    var parts = p.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        dict[parts[0]] = parts[1];
                    }
                }
                if (dict.Count > 0)
                {
                    config.DefaultHeaders = dict;
                }
            }

            return _cachedConfig = config;
        }
    }

    private static string EnvOr(string? current, string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? current ?? string.Empty : value.Trim();
    }

    private static int IntEnvOr(int current, string key)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(key), out var parsed) ? parsed : current;
    }

    private static bool BoolEnvOr(bool current, string key)
    {
        return bool.TryParse(Environment.GetEnvironmentVariable(key), out var parsed) ? parsed : current;
    }
}
