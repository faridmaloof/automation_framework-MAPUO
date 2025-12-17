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
