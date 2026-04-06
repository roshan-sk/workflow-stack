using System.Net.Http.Json;

public class WorkflowServiceClient
{
    private readonly HttpClient _httpClient;

    public WorkflowServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // START
    public async Task StartWorkflowAsync(int entityId, string token)
    {
        var requestBody = new
        {
            entityType = "PurchaseRequest",
            entityId = entityId
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "workflows/start"
        );

        request.Content = JsonContent.Create(requestBody);
        request.Headers.Add("Authorization", token);

        var response = await _httpClient.SendAsync(request);

        // var response = await _httpClient.PostAsJsonAsync(
        //     "workflows/start",
        //     request
        // );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Start workflow failed: {error}");
        }
    }

    // SIGNAL
    public async Task SendSignalAsync(int entityId, string signalName, string decision, string token)
    {
        var requestBody = new
        {
            entityType = "PurchaseRequest",
            entityId = entityId,
            signalName = signalName,
            payload = new Dictionary<string, object>
            {
                ["decision"] = decision
            }
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "workflows/signal"
        );
        
        request.Content = JsonContent.Create(requestBody);
        request.Headers.Add("Authorization", token);

        var response = await _httpClient.SendAsync(request);

        // var response = await _httpClient.PostAsJsonAsync(
        //     "workflows/signal",
        //     request
        // );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Signal failed: {error}");
        }
    }
}