using System;
using Microsoft.Extensions.DependencyInjection;
using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Infrastructure.Web;
using MAPUO.Infrastructure.API;

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
    /// <param name="webConfig">Configuración web</param>
    /// <param name="browserType">Tipo de navegador: "chromium", "firefox", "webkit"</param>
    /// <param name="headless">Si el navegador se ejecuta en modo headless</param>
    /// <returns>Service Provider configurado</returns>
    public static IServiceProvider Build(WebConfig webConfig, string browserType = "chromium", bool headless = false)
    {
        var services = new ServiceCollection();

        // Registrar configuración
        services.AddSingleton(webConfig);

        // Registrar habilidades (Abilities) como Scoped con parámetros
        services.AddScoped<IWebAbility>(sp => new PlaywrightWebAbility(webConfig, browserType, headless));

        // Registrar factory de actores con parámetros adicionales
        services.AddScoped<Func<string, string?, string?, IActor>>(sp => 
            (actorName, scenarioName, currentBrowser) => 
            {
                var webAbility = new PlaywrightWebAbility(webConfig, currentBrowser ?? browserType, headless, scenarioName);
                var tempServices = new ServiceCollection();
                tempServices.AddScoped<IWebAbility>(_ => webAbility);
                tempServices.AddScoped<Func<string, IActor>>(_ => (name) => new Actor(name, sp));
                var tempProvider = tempServices.BuildServiceProvider();
                return new Actor(actorName, tempProvider);
            }
        );

        return services.BuildServiceProvider();
    }

    public static IServiceProvider BuildApi(ApiConfig apiConfig)
    {
        var services = new ServiceCollection();

        services.AddSingleton(apiConfig);
        services.AddHttpClient("apiClient", client =>
        {
            if (!string.IsNullOrWhiteSpace(apiConfig.BaseUrl))
            {
                client.BaseAddress = new Uri(apiConfig.BaseUrl);
            }
            client.Timeout = TimeSpan.FromMilliseconds(apiConfig.TimeoutMs);
        });

        services.AddScoped<IApiAbility>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("apiClient");
            return new HttpApiAbility(apiConfig, client);
        });

        services.AddScoped<Func<string, IActor>>(sp => name => new Actor(name, sp));

        return services.BuildServiceProvider();
    }

    public static IActor CreateApiActor(IServiceProvider serviceProvider, string actorName)
    {
        var factory = serviceProvider.GetRequiredService<Func<string, IActor>>();
        return factory(actorName);
    }

    /// <summary>
    /// Crea un actor con las habilidades configuradas.
    /// </summary>
    /// <param name="serviceProvider">Service provider configurado</param>
    /// <param name="actorName">Nombre del actor</param>
    /// <param name="scenarioName">Nombre del escenario para evidencias</param>
    /// <param name="currentBrowser">Navegador actual para la ejecución</param>
    /// <returns>Instancia del actor</returns>
    public static IActor CreateActor(IServiceProvider serviceProvider, string actorName, string? scenarioName = null, string? currentBrowser = null)
    {
        var actorFactory = serviceProvider.GetRequiredService<Func<string, string?, string?, IActor>>();
        return actorFactory(actorName, scenarioName, currentBrowser);
    }
}
