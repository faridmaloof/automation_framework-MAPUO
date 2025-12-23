using System.Net;
using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Core.Tasks;

namespace MAPUO.Infrastructure.Web.Tasks;

/// <summary>
/// Tarea: Navegar al buscador DuckDuckGo.
/// </summary>
public class NavigateToGoogle : ITask
{
    public string Description => "Navegar a DuckDuckGo";

    public async Task ExecuteAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        await webAbility.NavigateToAsync("https://duckduckgo.com");
        
        // Esperar a que el cuadro de búsqueda esté visible
        await webAbility.WaitForSelectorAsync("input[name='q']");
    }
}

/// <summary>
/// Tarea: Buscar un término en DuckDuckGo.
/// </summary>
public class SearchOnGoogle : ITask
{
    private readonly string _searchTerm;

    public string Description => $"Buscar '{_searchTerm}' en DuckDuckGo";

    public SearchOnGoogle(string searchTerm)
    {
        _searchTerm = searchTerm ?? throw new ArgumentNullException(nameof(searchTerm));
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        
        // Navegar directamente a la URL de resultados para evitar bloqueos de UI
        var encodedTerm = WebUtility.UrlEncode(_searchTerm);
        var searchUrl = $"https://duckduckgo.com/?q={encodedTerm}&ia=web";
        await webAbility.NavigateToAsync(searchUrl);
        
        // Esperar a que se carguen los resultados (CSS combina layout normal y versión ligera)
        await webAbility.WaitForSelectorAsync("article[data-testid='result'], .result", 15000);
    }
}

/// <summary>
/// Tarea combinada: Navegar a DuckDuckGo y realizar una búsqueda.
/// Ejemplo de tarea compuesta que reutiliza otras tareas.
/// </summary>
public class PerformGoogleSearch : ITask
{
    private readonly string _searchTerm;

    public string Description => $"Realizar búsqueda completa de '{_searchTerm}' en DuckDuckGo";

    public PerformGoogleSearch(string searchTerm)
    {
        _searchTerm = searchTerm ?? throw new ArgumentNullException(nameof(searchTerm));
    }

    public async Task ExecuteAsync(IActor actor)
    {
        // Ejecutar tareas de forma secuencial
        await actor.ExecuteAsync(new NavigateToGoogle());
        await actor.ExecuteAsync(new SearchOnGoogle(_searchTerm));
    }
}
