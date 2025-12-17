using TechTalk.SpecFlow;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;
using MAPUO.Infrastructure.DI;
using MAPUO.Infrastructure.Web;
using System.Text.Json;

namespace MAPUO.StepDefinitions;

[Binding]
public class Hooks
{
    private IServiceProvider? _serviceProvider;
    private IActor? _actor;

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        // Cargar configuración desde archivo JSON o variables de entorno
        var webConfig = LoadWebConfig();

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
        
        Console.WriteLine($"=== Iniciando escenario: {scenarioContext.ScenarioInfo.Title} ===");
        Console.WriteLine($"Browser: {currentBrowser}, Headless: {webConfig.Headless}");
        Console.WriteLine($"Execution Timeout: {webConfig.ExecutionTimeoutMs}ms, Element Wait Timeout: {webConfig.ElementWaitTimeoutMs}ms");
        Console.WriteLine($"Evidencias - Video: {webConfig.RecordVideo}, Screenshots: Before={webConfig.ScreenshotsBeforeStep}, After={webConfig.ScreenshotsAfterStep}, Failure={webConfig.ScreenshotsOnFailure}");
    }

    private static string GetCurrentBrowser(WebConfig config)
    {
        // Verificar si hay un navegador especificado en variables de entorno (para ejecución paralela)
        var browserEnv = Environment.GetEnvironmentVariable("CURRENT_BROWSER");
        if (!string.IsNullOrEmpty(browserEnv) && config.Browsers.Contains(browserEnv))
        {
            return browserEnv;
        }
        
        // Si solo hay un navegador, usarlo
        if (config.Browsers.Count == 1)
        {
            return config.Browsers[0];
        }
        
        // Por defecto, usar el primer navegador de la lista
        return config.Browsers[0];
    }

    private static WebConfig LoadWebConfig()
    {
        var config = new WebConfig();

        // Intentar cargar desde archivo JSON
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webconfig.json");
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
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

        // Sobrescribir con variables de entorno si están definidas
        if (int.TryParse(Environment.GetEnvironmentVariable("EXECUTION_TIMEOUT_MS"), out var execTimeout))
        {
            config.ExecutionTimeoutMs = execTimeout;
        }
        if (int.TryParse(Environment.GetEnvironmentVariable("ELEMENT_WAIT_TIMEOUT_MS"), out var elementTimeout))
        {
            config.ElementWaitTimeoutMs = elementTimeout;
        }

        // Configuración del navegador desde variables de entorno
        var browserEnv = Environment.GetEnvironmentVariable("BROWSER");
        if (!string.IsNullOrEmpty(browserEnv))
        {
            config.BrowserType = browserEnv;
        }

        var headlessEnv = Environment.GetEnvironmentVariable("HEADLESS");
        if (bool.TryParse(headlessEnv, out var headless))
        {
            config.Headless = headless;
        }

        // Configuración de evidencias desde variables de entorno
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
        if (!string.IsNullOrEmpty(evidencePathEnv))
        {
            config.EvidenceBasePath = evidencePathEnv;
        }

        return config;
    }

    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        try
        {
            // Tomar screenshot si el escenario falló
            if (scenarioContext.TestError != null && _actor != null)
            {
                try
                {
                    var webAbility = _actor.GetAbility<IWebAbility>();
                    string screenshotPath = Path.Combine(
                        "TestResults", 
                        "Screenshots", 
                        $"{scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                    );

                    Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
                    await webAbility.TakeScreenshotAsync(screenshotPath);
                    
                    Console.WriteLine($"Screenshot guardado en: {screenshotPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"No se pudo tomar screenshot: {ex.Message}");
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

            Console.WriteLine($"=== Fin del escenario: {scenarioContext.ScenarioInfo.Title} ===");
        }
    }
}
