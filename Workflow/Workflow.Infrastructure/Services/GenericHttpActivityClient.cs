using System.Text;
using System.Text.Json;
using Workflow.Application.Interfaces;

namespace Workflow.Infrastructure.Services;

public class GenericHttpActivityClient : IGenericHttpActivityClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GenericHttpActivityClient(HttpClient http) => _http = http;

    public async Task PostAsync(string baseUrl, string relativeEndpoint, int entityId,
        Dictionary<string, object>? payload = null, string? token = null)
    {
        var url = BuildUrl(baseUrl, relativeEndpoint, entityId);
        var content = BuildContent(entityId, payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        // 🔐 ADD TOKEN HERE
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        var response = await _http.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }

    public async Task<T?> PostAsync<T>(string baseUrl, string relativeEndpoint, int entityId,
        Dictionary<string, object>? payload = null)
    {
        var url = BuildUrl(baseUrl, relativeEndpoint, entityId);
        var content = BuildContent(entityId, payload);
        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static string BuildUrl(string baseUrl, string relativeEndpoint, int entityId)
        => $"{baseUrl.TrimEnd('/')}/{relativeEndpoint.Replace("{entityId}", entityId.ToString()).TrimStart('/')}";

    private static StringContent BuildContent(int entityId, Dictionary<string, object>? payload)
    {
        var body = new { entityId, payload };
        var json = JsonSerializer.Serialize(body, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}