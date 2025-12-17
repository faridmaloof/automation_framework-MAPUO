using Microsoft.Extensions.DependencyInjection;
using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Infrastructure.Web;

namespace MAPUO.Infrastructure.DI;

/// <summary>
/// Clase de bootstrapping para configurar el contenedor de Dependency Injection.
/// Registra todas las habilidades y servicios necesarios para el framework.
/// </summary>
public static class ContainerBootstrapper
{
    /// <summary>
    /// Construye y configura el Service Provider con todas las dependencias.
    /// </summary>
    /// <param name="browserType">Tipo de navegador: "chromium", "firefox", "webkit"</param>
    /// <param name="headless">Si el navegador se ejecuta en modo headless</param>
    /// <returns>Service Provider configurado</returns>
    public static IServiceProvider Build(string browserType = "chromium", bool headless = false)
    {
        var services = new ServiceCollection();

        // Registrar habilidades (Abilities) como Scoped
        services.AddScoped<IWebAbility>(sp => new PlaywrightWebAbility(browserType, headless));

        // Registrar factory de actores
        services.AddScoped<Func<string, IActor>>(sp => 
            (actorName) => new Actor(actorName, sp)
        );

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Crea un actor con las habilidades configuradas.
    /// </summary>
    /// <param name="serviceProvider">Service provider configurado</param>
    /// <param name="actorName">Nombre del actor</param>
    /// <returns>Instancia del actor</returns>
    public static IActor CreateActor(IServiceProvider serviceProvider, string actorName)
    {
        var actorFactory = serviceProvider.GetRequiredService<Func<string, IActor>>();
        return actorFactory(actorName);
    }
}
