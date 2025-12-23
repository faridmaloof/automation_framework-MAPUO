using NUnit.Framework;
using TechTalk.SpecFlow;
using MAPUO.Core.Actors;
using MAPUO.Infrastructure.Web.Tasks;
using MAPUO.Infrastructure.Web.Questions;

namespace MAPUO.StepDefinitions;

[Binding]
public class GoogleSearchStepDefinitions
{
    private readonly IActor _actor;

    public GoogleSearchStepDefinitions(ScenarioContext scenarioContext)
    {
        // Obtener el actor del contexto de escenario
        _actor = scenarioContext.Get<IActor>("Actor");
    }

    [Given(@"que el usuario navega a la página de Google")]
    [Given(@"el usuario navega a la página de Google")]
    [Given(@"que el usuario navega al buscador")]
    [Given(@"el usuario navega al buscador")]
    public async Task GivenQueElUsuarioNavegaALaPaginaDeGoogle()
    {
        await _actor.ExecuteAsync(new NavigateToGoogle());
    }

    [When(@"busca el término ""(.*)""")]
    public async Task WhenBuscaElTermino(string searchTerm)
    {
        await _actor.ExecuteAsync(new SearchOnGoogle(searchTerm));
    }

    [Then(@"debe ver resultados de búsqueda")]
    public async Task ThenDebeVerResultadosDeBusqueda()
    {
        var resultsVisible = await _actor.AsksForAsync(new TheSearchResultsAreVisible());
        Assert.That(resultsVisible, Is.True, "Los resultados de búsqueda no son visibles");
    }

    [Then(@"la URL debe contener ""(.*)""")]
    public async Task ThenLaURLDebeContener(string expectedUrlPart)
    {
        var currentUrl = await _actor.AsksForAsync(new TheCurrentUrl());
        // Ajustar verificación para motores diferentes (por defecto DuckDuckGo)
        if (string.Equals(expectedUrlPart, "search", StringComparison.OrdinalIgnoreCase))
        {
            expectedUrlPart = "duckduckgo.com/?q=";
        }

        Assert.That(currentUrl, Does.Contain(expectedUrlPart), 
            $"La URL actual '{currentUrl}' no contiene '{expectedUrlPart}'");
    }
}
