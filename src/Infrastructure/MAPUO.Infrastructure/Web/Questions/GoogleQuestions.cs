using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Core.Questions;

namespace MAPUO.Infrastructure.Web.Questions;

/// <summary>
/// Pregunta: ¿Cuál es el título de la página actual?
/// </summary>
public class ThePageTitle : IQuestion<string>
{
    public string Description => "El título de la página actual";

    public async Task<string> AnswerAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        
        // Obtener el título de la página mediante el selector del elemento title
        try
        {
            return await webAbility.GetTextAsync("title");
        }
        catch
        {
            // Si no se puede obtener el título, retornar la URL
            return await webAbility.GetCurrentUrlAsync();
        }
    }
}

/// <summary>
/// Pregunta: ¿Los resultados de búsqueda están visibles?
/// </summary>
public class TheSearchResultsAreVisible : IQuestion<bool>
{
    public string Description => "¿Los resultados de búsqueda son visibles?";

    public async Task<bool> AnswerAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        
        // Verificar si los resultados de búsqueda están visibles (esperar por un título de resultado)
        string resultsSelector = "h3";
        
        try
        {
            await webAbility.WaitForSelectorAsync(resultsSelector, 5000);
            return await webAbility.IsVisibleAsync(resultsSelector);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Pregunta: ¿Cuál es la URL actual?
/// </summary>
public class TheCurrentUrl : IQuestion<string>
{
    public string Description => "La URL actual de la página";

    public async Task<string> AnswerAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        return await webAbility.GetCurrentUrlAsync();
    }
}

/// <summary>
/// Pregunta: ¿El texto está visible en la página?
/// </summary>
public class TheTextIsVisible : IQuestion<bool>
{
    private readonly string _text;

    public string Description => $"¿El texto '{_text}' es visible en la página?";

    public TheTextIsVisible(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public async Task<bool> AnswerAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        
        try
        {
            // Buscar el texto en el cuerpo de la página
            string selector = $"text={_text}";
            return await webAbility.IsVisibleAsync(selector);
        }
        catch
        {
            return false;
        }
    }
}
