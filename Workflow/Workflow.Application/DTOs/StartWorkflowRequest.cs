namespace Workflow.Application.DTOs;

public class StartWorkflowRequest
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int TenantId { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}