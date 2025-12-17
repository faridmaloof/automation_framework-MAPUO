using TechTalk.SpecFlow;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;
using MAPUO.Infrastructure.DI;

namespace MAPUO.StepDefinitions;

[Binding]
public class Hooks
{
    private IServiceProvider? _serviceProvider;
    private IActor? _actor;

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        // Configurar el contenedor de DI
        // Usar headless = false para ver el navegador (útil para debugging)
        // Cambiar a true para ejecución en CI/CD
        bool headless = bool.TryParse(Environment.GetEnvironmentVariable("HEADLESS"), out var h) ? h : false;
        string browserType = Environment.GetEnvironmentVariable("BROWSER") ?? "chromium";

        _serviceProvider = ContainerBootstrapper.Build(browserType, headless);
        
        // Crear el actor
        _actor = ContainerBootstrapper.CreateActor(_serviceProvider, "TestUser");
        
        // Almacenar el actor en el contexto del escenario
        scenarioContext.Set(_actor, "Actor");
        
        Console.WriteLine($"=== Iniciando escenario: {scenarioContext.ScenarioInfo.Title} ===");
        Console.WriteLine($"Browser: {browserType}, Headless: {headless}");
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
