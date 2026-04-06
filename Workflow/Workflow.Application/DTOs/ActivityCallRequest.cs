namespace Workflow.Application.DTOs;

public class ActivityCallRequest
{
    public int EntityId { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}