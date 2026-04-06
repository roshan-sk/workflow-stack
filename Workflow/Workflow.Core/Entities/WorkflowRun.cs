using Workflow.Core.Enums;

namespace Workflow.Core.Entities;

public class WorkflowRun
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string TemporalWorkflowId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Started;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}