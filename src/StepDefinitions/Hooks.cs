using TechTalk.SpecFlow;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;
using MAPUO.Infrastructure.DI;
using MAPUO.Infrastructure.Web;
using System.Text.Json;
using Allure.Net.Commons;
using NUnit.Framework;
using System.Linq;

namespace MAPUO.StepDefinitions;

[Binding]
public class Hooks
{
    private readonly FeatureContext _featureContext;
    private IServiceProvider? _serviceProvider;
    private IActor? _actor;
    private static readonly AllureLifecycle _allure = AllureLifecycle.Instance;
    private static WebConfig? _cachedConfig;
    private static readonly object _configLock = new();

    public Hooks(FeatureContext featureContext)
    {
        _featureContext = featureContext;
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        // Cargar configuración desde archivo JSON o variables de entorno
        var webConfig = LoadWebConfig();

        var allTags = (scenarioContext.ScenarioInfo.Tags ?? Array.Empty<string>())
            .Concat(_featureContext.FeatureInfo.Tags ?? Array.Empty<string>());

        // Filtro por tags configurados
        if (webConfig.Tags.Any())
        {
            // Incluir tags de escenario y de característica para que los filtros funcionen en ambos niveles
            var scenarioTags = allTags
                .Select(t => t.Trim().TrimStart('@').ToLowerInvariant())
                .ToList();
            var required = webConfig.Tags
                .Select(t => t.Trim().TrimStart('@').ToLowerInvariant());

            if (!required.Any(scenarioTags.Contains))
            {
                Assert.Ignore($"Escenario omitido por filtro de tags: {string.Join(',', webConfig.Tags)}");
            }
        }

        // Escenarios API se manejan en ApiHooks
        if (allTags.Any(t => t.Equals("api", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        // Si no hay navegadores especificados, usar el navegador por defecto
        if (webConfig.Browsers.Count == 0)
        {
            webConfig.Browsers.Add(webConfig.BrowserType);
        }

        // Para ejecución automática en múltiples navegadores, determinar el navegador actual
        var currentBrowser = GetCurrentBrowser(webConfig);
        
        // Configurar el contenedor de DI
        _serviceProvider = ContainerBootstrapper.Build(webConfig, currentBrowser, webConfig.Headless);
        
        // Crear el actor con el nombre del escenario para evidencias
        var scenarioName = scenarioContext.ScenarioInfo.Title.Replace(" ", "_");
        _actor = ContainerBootstrapper.CreateActor(_serviceProvider, "TestUser", scenarioName, currentBrowser);
        
        // Almacenar el actor en el contexto del escenario
        scenarioContext.Set(_actor, "Actor");
        
        // NOTA: Allure metadata se agregará desde el convertidor TRX→Allure en RunTests-Fixed.ps1
        // EnrichAllureMetadata(scenarioContext, currentBrowser, webConfig);
        
        Console.WriteLine($"\n=== Iniciando escenario: {scenarioContext.ScenarioInfo.Title} ===");
        Console.WriteLine($"Browser: {currentBrowser}, Headless: {webConfig.Headless}");
        Console.WriteLine($"Execution Timeout: {webConfig.ExecutionTimeoutMs}ms, Element Wait Timeout: {webConfig.ElementWaitTimeoutMs}ms");
        Console.WriteLine($"Evidencias - Video: {webConfig.RecordVideo}, Screenshots: Before={webConfig.ScreenshotsBeforeStep}, After={webConfig.ScreenshotsAfterStep}, Failure={webConfig.ScreenshotsOnFailure}");
    }

    private static string GetCurrentBrowser(WebConfig config)
    {
        // Priorizar CURRENT_BROWSER o BROWSER si existen
        var browserEnv = Environment.GetEnvironmentVariable("CURRENT_BROWSER") ?? Environment.GetEnvironmentVariable("BROWSER");
        if (!string.IsNullOrWhiteSpace(browserEnv))
        {
            return browserEnv.Trim();
        }

        // Si hay lista configurada, usar el primero
        if (config.Browsers.Count > 0)
        {
            return config.Browsers[0];
        }

        // Fallback al BrowserType (o chromium por defecto)
        return string.IsNullOrWhiteSpace(config.BrowserType) ? "chromium" : config.BrowserType;
    }

    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        try
        {
            var webConfig = LoadWebConfig();

            var allTags = (scenarioContext.ScenarioInfo.Tags ?? Array.Empty<string>())
                .Concat(_featureContext.FeatureInfo.Tags ?? Array.Empty<string>());

            // Escenarios API se manejan en ApiHooks
            if (allTags.Any(t => t.Equals("api", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            var currentBrowser = GetCurrentBrowser(webConfig);
            
            // Tomar screenshot si está habilitado (falló el escenario y ScreenshotsOnFailure=true)
            if (scenarioContext.TestError != null && _actor != null && webConfig.ScreenshotsOnFailure)
            {
                try
                {
                    var webAbility = _actor.GetAbility<IWebAbility>();
                    
                    // Usar EvidenceBasePath de la configuración
                    var evidenceDir = Path.Combine(webConfig.EvidenceBasePath, "screenshots");
                    var screenshotFileName = $"{scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{currentBrowser}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var screenshotPath = Path.Combine(evidenceDir, screenshotFileName);

                    Directory.CreateDirectory(evidenceDir);
                    await webAbility.TakeScreenshotAsync(screenshotPath);
                    
                    // Verificar que el screenshot se guardó correctamente
                    if (File.Exists(screenshotPath))
                    {
                        var fileSize = new FileInfo(screenshotPath).Length / 1024;
                        Console.WriteLine($"✓ Screenshot guardado: {screenshotPath}");
                        Console.WriteLine($"  Tamaño: {fileSize} KB");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ No se pudo tomar screenshot: {ex.Message}");
                }
                
                // Guardar error details en archivo de texto
                try
                {
                    var errorDir = Path.Combine(webConfig.EvidenceBasePath, "errors");
                    var errorFileName = $"{scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{currentBrowser}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    var errorPath = Path.Combine(errorDir, errorFileName);
                    
                    var errorDetails = $"Scenario: {scenarioContext.ScenarioInfo.Title}\n" +
                                     $"Browser: {currentBrowser}\n" +
                                     $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                                     $"Error Type: {scenarioContext.TestError.GetType().Name}\n\n" +
                                     $"Message: {scenarioContext.TestError.Message}\n\n" +
                                     $"Stack Trace:\n{scenarioContext.TestError.StackTrace}";
                    
                    Directory.CreateDirectory(errorDir);
                    await File.WriteAllTextAsync(errorPath, errorDetails);
                    
                    if (File.Exists(errorPath))
                    {
                        Console.WriteLine($"✓ Error details guardados: {errorPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ No se pudieron guardar error details: {ex.Message}");
                }
            }

            // Cerrar el navegador
            if (_actor != null && _actor.HasAbility<IWebAbility>())
            {
                var webAbility = _actor.GetAbility<IWebAbility>();
                await webAbility.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en AfterScenario: {ex.Message}");
        }
        finally
        {
            // Liberar recursos del service provider
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Console.WriteLine($"\n=== Fin del escenario: {scenarioContext.ScenarioInfo.Title} ===");
            
            // Mostrar resumen de evidencias si hubo error
            if (scenarioContext.TestError != null)
            {
                var webConfig = LoadWebConfig();
                var evidenceBase = Path.Combine(Directory.GetCurrentDirectory(), webConfig.EvidenceBasePath);
                Console.WriteLine($"📁 Directorio de evidencias: {evidenceBase}");
                Console.WriteLine($"   - Screenshots: {Path.Combine(evidenceBase, "screenshots")}");
                Console.WriteLine($"   - Errors: {Path.Combine(evidenceBase, "errors")}");
            }
            
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Enriquece el reporte Allure con metadata profesional para trazabilidad y análisis
    /// </summary>
    private static void EnrichAllureMetadata(ScenarioContext context, string browser, WebConfig config)
    {
        try
        {
            // Agregar parámetros de ambiente
            _allure.UpdateTestCase(testResult =>
            {
                // Browser y plataforma
                testResult.parameters.Add(new Parameter { name = "Browser", value = browser });
                testResult.parameters.Add(new Parameter { name = "Headless", value = config.Headless.ToString() });
                testResult.parameters.Add(new Parameter { name = "Platform", value = Environment.OSVersion.ToString() });
                
                // Timeouts configurados
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Execution Timeout", 
                    value = $"{config.ExecutionTimeoutMs}ms" 
                });
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Element Wait Timeout", 
                    value = $"{config.ElementWaitTimeoutMs}ms" 
                });
                
                // Evidencias habilitadas
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Record Video", 
                    value = config.RecordVideo.ToString() 
                });
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Screenshots On Failure", 
                    value = config.ScreenshotsOnFailure.ToString() 
                });
                
                // Metadata de ejecución
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Execution Time", 
                    value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
                });
                
                testResult.parameters.Add(new Parameter 
                { 
                    name = "Test Environment", 
                    value = Environment.GetEnvironmentVariable("TEST_ENV") ?? "local" 
                });
                
                // Tags del escenario como labels
                foreach (var tag in context.ScenarioInfo.Tags)
                {
                    testResult.labels.Add(new Label { name = "tag", value = tag });
                }
                
                // Categorización por tipo de prueba
                if (context.ScenarioInfo.Tags.Any(t => t.Contains("smoke", StringComparison.OrdinalIgnoreCase)))
                {
                    testResult.labels.Add(new Label { name = "suite", value = "Smoke Tests" });
                }
                else if (context.ScenarioInfo.Tags.Any(t => t.Contains("regression", StringComparison.OrdinalIgnoreCase)))
                {
                    testResult.labels.Add(new Label { name = "suite", value = "Regression Tests" });
                }
                else
                {
                    testResult.labels.Add(new Label { name = "suite", value = "Functional Tests" });
                }
                
                // Severidad basada en tags
                if (context.ScenarioInfo.Tags.Any(t => t.Contains("critical", StringComparison.OrdinalIgnoreCase)))
                {
                    testResult.labels.Add(new Label { name = "severity", value = "critical" });
                }
                else if (context.ScenarioInfo.Tags.Any(t => t.Contains("high", StringComparison.OrdinalIgnoreCase)))
                {
                    testResult.labels.Add(new Label { name = "severity", value = "blocker" });
                }
                else
                {
                    testResult.labels.Add(new Label { name = "severity", value = "normal" });
                }
            });
            
            Console.WriteLine($"Metadata Allure enriquecida para: {context.ScenarioInfo.Title}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enriquecer Allure metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga la configuración web desde archivo JSON y variables de entorno
    /// </summary>
    private static WebConfig LoadWebConfig()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        lock (_configLock)
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var config = new WebConfig();

            // Intentar cargar desde archivo JSON en output (SpecFlow copia webconfig.json)
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webconfig.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var loadedConfig = JsonSerializer.Deserialize<WebConfig>(json);
                    if (loadedConfig != null)
                    {
                        config = loadedConfig;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar configuración desde {configPath}: {ex.Message}");
                }
            }

            // Overrides por variables de entorno
            if (int.TryParse(Environment.GetEnvironmentVariable("EXECUTION_TIMEOUT_MS"), out var execTimeout))
            {
                config.ExecutionTimeoutMs = execTimeout;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("ELEMENT_WAIT_TIMEOUT_MS"), out var elementTimeout))
            {
                config.ElementWaitTimeoutMs = elementTimeout;
            }

            var headlessEnv = Environment.GetEnvironmentVariable("HEADLESS");
            if (bool.TryParse(headlessEnv, out var headless))
            {
                config.Headless = headless;
            }

            var recordVideoEnv = Environment.GetEnvironmentVariable("RECORD_VIDEO");
            if (bool.TryParse(recordVideoEnv, out var recordVideo))
            {
                config.RecordVideo = recordVideo;
            }

            var screenshotsBeforeEnv = Environment.GetEnvironmentVariable("SCREENSHOTS_BEFORE_STEP");
            if (bool.TryParse(screenshotsBeforeEnv, out var screenshotsBefore))
            {
                config.ScreenshotsBeforeStep = screenshotsBefore;
            }

            var screenshotsAfterEnv = Environment.GetEnvironmentVariable("SCREENSHOTS_AFTER_STEP");
            if (bool.TryParse(screenshotsAfterEnv, out var screenshotsAfter))
            {
                config.ScreenshotsAfterStep = screenshotsAfter;
            }

            var screenshotsFailureEnv = Environment.GetEnvironmentVariable("SCREENSHOTS_ON_FAILURE");
            if (bool.TryParse(screenshotsFailureEnv, out var screenshotsFailure))
            {
                config.ScreenshotsOnFailure = screenshotsFailure;
            }

            var evidencePathEnv = Environment.GetEnvironmentVariable("EVIDENCE_BASE_PATH");
            if (!string.IsNullOrWhiteSpace(evidencePathEnv))
            {
                config.EvidenceBasePath = evidencePathEnv.Trim();
            }

            var generateReportEnv = Environment.GetEnvironmentVariable("GENERATE_REPORT");
            if (bool.TryParse(generateReportEnv, out var generateReport))
            {
                config.GenerateReport = generateReport;
            }

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

            // Browser/browsers
            var browsersEnv = Environment.GetEnvironmentVariable("BROWSERS");
            if (!string.IsNullOrWhiteSpace(browsersEnv))
            {
                var list = browsersEnv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (list.Count > 0)
                {
                    config.Browsers = list;
                }
            }

            var browserEnv = Environment.GetEnvironmentVariable("BROWSER");
            if (!string.IsNullOrWhiteSpace(browserEnv))
            {
                config.BrowserType = browserEnv.Trim();
            }

            // Normalizar lista de browsers si sigue vacía
            if (config.Browsers == null)
            {
                config.Browsers = new List<string>();
            }

            if (config.Browsers.Count == 0)
            {
                config.Browsers.Add(string.IsNullOrWhiteSpace(config.BrowserType) ? "chromium" : config.BrowserType);
            }

            // Defaults finales
            if (string.IsNullOrWhiteSpace(config.EvidenceBasePath))
            {
                config.EvidenceBasePath = "TestResults/Evidence";
            }

            return _cachedConfig = config;
        }
    }
}
