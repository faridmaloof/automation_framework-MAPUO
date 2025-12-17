using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Core.Tasks;

namespace MAPUO.Infrastructure.Web.Tasks;

/// <summary>
/// Tarea: Navegar a Google.
/// </summary>
public class NavigateToGoogle : ITask
{
    public string Description => "Navegar a la página de Google";

    public async Task ExecuteAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        await webAbility.NavigateToAsync("https://www.google.com");
        
        // Esperar a que la página cargue completamente
        await Task.Delay(1000);
    }
}

/// <summary>
/// Tarea: Buscar un término en Google.
/// </summary>
public class SearchOnGoogle : ITask
{
    private readonly string _searchTerm;

    public string Description => $"Buscar '{_searchTerm}' en Google";

    public SearchOnGoogle(string searchTerm)
    {
        _searchTerm = searchTerm ?? throw new ArgumentNullException(nameof(searchTerm));
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        
        // Selector del campo de búsqueda de Google
        string searchBoxSelector = "textarea[name='q']";
        
        // Esperar a que el cuadro de búsqueda esté visible
        await webAbility.WaitForSelectorAsync(searchBoxSelector, 10000);
        
        // Ingresar el término de búsqueda
        await webAbility.FillAsync(searchBoxSelector, _searchTerm);
        
        // Presionar Enter para buscar
        await webAbility.PressKeyAsync(searchBoxSelector, "Enter");
        
        // Esperar a que se carguen los resultados
        await Task.Delay(2000);
    }
}

/// <summary>
/// Tarea combinada: Navegar a Google y realizar una búsqueda.
/// Ejemplo de tarea compuesta que reutiliza otras tareas.
/// </summary>
public class PerformGoogleSearch : ITask
{
    private readonly string _searchTerm;

    public string Description => $"Realizar búsqueda completa de '{_searchTerm}' en Google";

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
