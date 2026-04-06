namespace Workflow.Application.DTOs;

public class SignalWorkflowRequest
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string SignalName { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; }
}