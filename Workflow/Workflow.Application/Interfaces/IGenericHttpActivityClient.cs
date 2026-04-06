namespace Workflow.Application.Interfaces;

public interface IGenericHttpActivityClient
{
    Task PostAsync(string baseUrl, string relativeEndpoint, int entityId, Dictionary<string, object>? payload = null, string? token = null);
    Task<T?> PostAsync<T>(string baseUrl, string relativeEndpoint, int entityId, Dictionary<string, object>? payload = null);
}