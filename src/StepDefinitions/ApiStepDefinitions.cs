using System.Text.Json;
using MAPUO.Core.Abilities;
using MAPUO.Core.Actors;
using MAPUO.Infrastructure.API;
using TechTalk.SpecFlow;
using NUnit.Framework;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace MAPUO.StepDefinitions;

[Binding]
public class ApiStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private IActor Actor => _scenarioContext.Get<IActor>("Actor");
    private IApiAbility Api => Actor.GetAbility<IApiAbility>();
    private ApiConfig ApiConfig => _scenarioContext.TryGetValue("ApiConfig", out ApiConfig cfg) ? cfg : new ApiConfig();

    public ApiStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("que configuro el header \"(.*)\" con valor \"(.*)\"")]
    public void GivenConfiguroHeader(string name, string value)
    {
        Api.SetHeader(name, value);
    }

    [When("realizo un GET a \"(.*)\"")]
    public async Task WhenRealizoUnGET(string endpoint)
    {
        var response = await Api.GetAsync<JsonElement>(endpoint);
        _scenarioContext.Set(response, "api_response");
        _scenarioContext.Set(Api.GetLastStatusCode(), "api_status");
        SaveApiEvidence("GET", response);
    }

    [When("realizo un GET a \"(.*)\" con parámetros")]
    public async Task WhenRealizoUnGETConParametros(string endpoint, Table table)
    {
        var urlWithParams = AppendParameters(endpoint, table);
        var response = await Api.GetAsync<JsonElement>(urlWithParams);
        _scenarioContext.Set(response, "api_response");
        _scenarioContext.Set(Api.GetLastStatusCode(), "api_status");
        SaveApiEvidence("GET", response, table);
    }

    [When("realizo un POST a \"(.*)\" con cuerpo json")] 
    public async Task WhenRealizoUnPOST(string endpoint, string multilineText)
    {
        var doc = JsonSerializer.Deserialize<JsonElement>(multilineText);
        var response = await Api.PostAsync<JsonElement, JsonElement>(endpoint, doc);
        _scenarioContext.Set(response, "api_response");
        _scenarioContext.Set(Api.GetLastStatusCode(), "api_status");
        SaveApiEvidence("POST", response, body: multilineText);
    }

    [When("realizo un POST a \"(.*)\" con datos")]
    public async Task WhenRealizoUnPOSTConDatos(string endpoint, Table table)
    {
        var jsonBody = BuildJsonFromTable(table);
        var doc = JsonDocument.Parse(jsonBody).RootElement;
        var response = await Api.PostAsync<JsonElement, JsonElement>(endpoint, doc);
        _scenarioContext.Set(response, "api_response");
        _scenarioContext.Set(Api.GetLastStatusCode(), "api_status");
        SaveApiEvidence("POST", response, table, jsonBody);
    }

    [When("realizo un PUT a \"(.*)\" con datos")]
    public async Task WhenRealizoUnPUTConDatos(string endpoint, Table table)
    {
        var jsonBody = BuildJsonFromTable(table);
        var doc = JsonDocument.Parse(jsonBody).RootElement;
        var response = await Api.PutAsync<JsonElement, JsonElement>(endpoint, doc);
        _scenarioContext.Set(response, "api_response");
        _scenarioContext.Set(Api.GetLastStatusCode(), "api_status");
        SaveApiEvidence("PUT", response, table, jsonBody);
    }

    [Then("el codigo de respuesta debe ser (.*)")]
    public void ThenCodigoRespuesta(int expectedStatus)
    {
        var status = _scenarioContext.Get<int>("api_status");
        Assert.That(status, Is.EqualTo(expectedStatus));
    }

    [Then("la respuesta tiene la propiedad \"(.*)\"")]
    public void ThenRespuestaTienePropiedad(string propertyPath)
    {
        var response = _scenarioContext.Get<JsonElement>("api_response");
        var exists = TryFindProperty(response, propertyPath);
        Assert.That(exists, Is.True, $"No se encontró la propiedad '{propertyPath}' en la respuesta");
    }

    private string AppendParameters(string endpoint, Table table)
    {
        if (table.Rows.Count == 0) return endpoint;

        var sb = new StringBuilder();
        foreach (var row in table.Rows)
        {
            var key = row.Values.FirstOrDefault();
            var value = row.Values.Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value ?? string.Empty));
        }

        if (sb.Length == 0) return endpoint;
        var separator = endpoint.Contains('?') ? '&' : '?';
        return $"{endpoint}{separator}{sb}";
    }

    private string BuildJsonFromTable(Table table)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            var key = row.Values.FirstOrDefault();
            var value = row.Values.Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key)) continue;
            dict[key] = value;
        }
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }

    private void SaveApiEvidence(string method, JsonElement response, Table? parameters = null, string? body = null)
    {
        // Almacenar evidencias en un path común (aplica para API/mobile/web)
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), ApiConfig.EvidenceBasePath, "api");
        Directory.CreateDirectory(baseDir);

        var slug = SanitizeFileName(_scenarioContext.ScenarioInfo.Title);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        var requestInfo = new StringBuilder();
        requestInfo.AppendLine($"Metodo: {Api.GetLastMethod() ?? method}");
        requestInfo.AppendLine($"URL: {Api.GetLastRequestUrl()}");
        requestInfo.AppendLine($"Status: {Api.GetLastStatusCode()}");
        if (parameters != null && parameters.Rows.Count > 0)
        {
            requestInfo.AppendLine("Parámetros:");
            foreach (var row in parameters.Rows)
            {
                var key = row.Values.FirstOrDefault();
                var value = row.Values.Skip(1).FirstOrDefault();
                requestInfo.AppendLine($"  - {key}: {value}");
            }
        }
        if (!string.IsNullOrWhiteSpace(body ?? Api.GetLastRequestBody()))
        {
            requestInfo.AppendLine("Body:");
            requestInfo.AppendLine(body ?? Api.GetLastRequestBody());
        }

        var responseContent = Api.GetLastResponseContent();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            responseContent = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }

        var requestPath = Path.Combine(baseDir, $"{slug}_{timestamp}_request.txt");
        var responsePath = Path.Combine(baseDir, $"{slug}_{timestamp}_response.json");

        File.WriteAllText(requestPath, requestInfo.ToString());
        File.WriteAllText(responsePath, responseContent);

        // Guardar lista de archivos para adjuntar en Allure u otros reportes
        var list = _scenarioContext.TryGetValue("api_evidence_files", out List<string> existing) ? existing : new List<string>();
        list.Add(requestPath);
        list.Add(responsePath);
        _scenarioContext.Set(list, "api_evidence_files");
    }

    private static string SanitizeFileName(string name)
    {
        var filtered = name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(filtered);
    }

    private static bool TryFindProperty(JsonElement element, string path)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = element;
        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
            {
                current = next;
                continue;
            }
            return false;
        }
        return true;
    }
}
