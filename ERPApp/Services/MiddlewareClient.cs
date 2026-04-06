using System.Net.Http.Json;
using System.Net.Http.Headers;

public class MiddlewareClient
{
    private readonly HttpClient _httpClient;

    public MiddlewareClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Middleware");
    }

    // START
    public async Task StartWorkflowAsync(int entityId, string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/purchase-request/start"
        );

        request.Content = JsonContent.Create(new { entityId });

        // 🔐 Forward JWT
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Middleware start failed: {error}");
        }
    }

    // MANAGER
    public async Task ManagerDecisionAsync(int entityId, string decision, string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/purchase-request/manager-decision"
        );

        request.Content = JsonContent.Create(new
        {
            entityId,
            decision
        });

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);

        var response = await _httpClient.SendAsync(request);
        // var response = await _httpClient.PostAsJsonAsync(
        //     "api/purchase-request/manager-decision",
        //     new { entityId, decision }
        // );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Manager decision failed: {error}");
        }
    }

    // FINANCE
    public async Task FinanceDecisionAsync(int entityId, string decision, string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/purchase-request/finance-decision"
        );

        request.Content = JsonContent.Create(new
        {
            entityId,
            decision
        });

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Finance decision failed: {error}");
        }
    }
    
    // HR
    public async Task HrDecisionAsync(int entityId, string decision, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/purchase-request/hr-decision");

        request.Content = JsonContent.Create(new
        {
            entityId,
            decision
        });

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Hr decision failed: {error}");
        }
    }
}