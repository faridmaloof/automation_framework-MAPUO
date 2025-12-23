using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAPUO.Core.Abilities;

namespace MAPUO.Infrastructure.API;

public class HttpApiAbility : IApiAbility
{
    private readonly ApiConfig _config;
    private readonly HttpClient _client;
    private int _lastStatusCode;
    private string? _lastRequestUrl;
    private string? _lastRequestBody;
    private string? _lastResponseContent;
    private string? _lastMethod;

    public string Name => "HttpApiAbility";

    public HttpApiAbility(ApiConfig config, HttpClient client)
    {
        _config = config;
        _client = client;
        ApplyAuth();
        ApplyDefaultHeaders();
    }

    public int GetLastStatusCode() => _lastStatusCode;
    public string? GetLastRequestUrl() => _lastRequestUrl;
    public string? GetLastMethod() => _lastMethod;
    public string? GetLastRequestBody() => _lastRequestBody;
    public string? GetLastResponseContent() => _lastResponseContent;

    public void SetHeader(string name, string value)
    {
        _client.DefaultRequestHeaders.Remove(name);
        _client.DefaultRequestHeaders.Add(name, value);
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        var url = Normalize(endpoint);
        _lastMethod = "GET";
        _lastRequestUrl = url;
        _lastRequestBody = null;

        var response = await _client.GetAsync(url);
        _lastStatusCode = (int)response.StatusCode;
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _lastResponseContent = content;
        return JsonSerializer.Deserialize<T>(content, JsonOptions())!;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions());
        var url = Normalize(endpoint);
        _lastMethod = "POST";
        _lastRequestUrl = url;
        _lastRequestBody = json;

        var response = await _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        _lastStatusCode = (int)response.StatusCode;
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _lastResponseContent = content;
        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions())!;
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions());
        var url = Normalize(endpoint);
        _lastMethod = "PUT";
        _lastRequestUrl = url;
        _lastRequestBody = json;

        var response = await _client.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        _lastStatusCode = (int)response.StatusCode;
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _lastResponseContent = content;
        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions())!;
    }

    public async Task DeleteAsync(string endpoint)
    {
        var url = Normalize(endpoint);
        _lastMethod = "DELETE";
        _lastRequestUrl = url;
        _lastRequestBody = null;

        var response = await _client.DeleteAsync(url);
        _lastStatusCode = (int)response.StatusCode;
        response.EnsureSuccessStatusCode();
        _lastResponseContent = await response.Content.ReadAsStringAsync();
    }

    private string Normalize(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return string.Empty;
        if (_client.BaseAddress != null && Uri.IsWellFormedUriString(endpoint, UriKind.Relative))
        {
            return new Uri(_client.BaseAddress, endpoint).ToString();
        }
        return endpoint;
    }

    private void ApplyAuth()
    {
        var auth = _config.AuthType?.ToLowerInvariant();
        if (auth == "bearer" && !string.IsNullOrWhiteSpace(_config.BearerToken))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.BearerToken);
        }
        else if (auth == "basic" && !string.IsNullOrWhiteSpace(_config.BasicUser) && !string.IsNullOrWhiteSpace(_config.BasicPassword))
        {
            var bytes = Encoding.UTF8.GetBytes($"{_config.BasicUser}:{_config.BasicPassword}");
            var token = Convert.ToBase64String(bytes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }

    private void ApplyDefaultHeaders()
    {
        foreach (var kvp in _config.DefaultHeaders)
        {
            SetHeader(kvp.Key, kvp.Value);
        }
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
