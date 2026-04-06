namespace Workflow.Application.DTOs;

public class WorkflowRegistryEntry
{
    public string EntityType { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string TaskQueue { get; set; } = string.Empty;
    public string WorkflowIdPattern { get; set; } = string.Empty;
}