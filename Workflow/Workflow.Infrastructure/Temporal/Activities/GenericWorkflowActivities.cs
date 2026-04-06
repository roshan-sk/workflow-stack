using Microsoft.Extensions.Configuration;
using Temporalio.Activities;
using Workflow.Application.Interfaces;
using Workflow.Core.Entities;
using Workflow.Core.Enums;

namespace Workflow.Infrastructure.Temporal.Activities;

public class GenericWorkflowActivities
{
    private readonly IGenericHttpActivityClient _http;
    private readonly IConfiguration _config;
    private readonly IWorkflowRunRepository _repository;

    public GenericWorkflowActivities(IGenericHttpActivityClient http, IConfiguration config, IWorkflowRunRepository repository)
    {
        _http = http;
        _config = config;
        _repository = repository;
    }

    [Activity]
    public async Task ExecuteActivityAsync(
        string activityKey,
        int entityId,
        Dictionary<string, object>? payload = null)
    {
        var serviceToken = Environment.GetEnvironmentVariable("SERVICE_TOKEN");

        var parts = activityKey.Split(':');

        if (parts.Length != 2)
            throw new InvalidOperationException($"Invalid ActivityKey format: {activityKey}");

        var entity = parts[0];
        var action = parts[1];

        var baseUrl = _config[$"ActivityEndpoints:{entity}:{action}:BaseUrl"]
            ?? throw new InvalidOperationException(
                $"Missing config: ActivityEndpoints:{entity}:{action}:BaseUrl");

        var endpoint = _config[$"ActivityEndpoints:{entity}:{action}:Endpoint"]
            ?? throw new InvalidOperationException(
                $"Missing config: ActivityEndpoints:{entity}:{action}:Endpoint");
        
        var url = $"{baseUrl}/{endpoint.Replace("{id}", entityId.ToString())}";

        await _http.PostAsync(baseUrl, endpoint, entityId, payload, serviceToken);
    }

    

    [Activity]
    public async Task<Dictionary<string, object>?> ExecuteActivityWithResultAsync(
        string activityKey,
        int entityId,
        Dictionary<string, object>? payload = null)
    {
        var parts = activityKey.Split(':');

        if (parts.Length != 2)
            throw new InvalidOperationException($"Invalid ActivityKey format: {activityKey}");

        var entity = parts[0];
        var action = parts[1];

        var baseUrl = _config[$"ActivityEndpoints:{entity}:{action}:BaseUrl"]
            ?? throw new InvalidOperationException(
                $"Missing config: ActivityEndpoints:{entity}:{action}:BaseUrl");

        var endpoint = _config[$"ActivityEndpoints:{entity}:{action}:Endpoint"]
            ?? throw new InvalidOperationException(
                $"Missing config: ActivityEndpoints:{entity}:{action}:Endpoint");

        return await _http.PostAsync<Dictionary<string, object>>(baseUrl, endpoint, entityId, payload);
    }

    [Activity]
    public async Task MarkWorkflowCompletedAsync(string workflowId)
    {
        await _repository.UpdateStatusAsync(workflowId, WorkflowStatus.Completed);
    }

    [Activity]
    public async Task MarkWorkflowRejectedAsync(string workflowId)
    {
        await _repository.UpdateStatusAsync(workflowId, WorkflowStatus.Rejected);
    }

    [Activity]
    public async Task MarkWorkflowFailedAsync(string workflowId)
    {
        await _repository.UpdateStatusAsync(workflowId, WorkflowStatus.Failed);
    }
}