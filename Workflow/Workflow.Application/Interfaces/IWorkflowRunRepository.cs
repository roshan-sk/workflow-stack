using Workflow.Core.Entities;
using Workflow.Core.Enums;

namespace Workflow.Application.Interfaces;

public interface IWorkflowRunRepository
{
    Task AddAsync(WorkflowRun run);
    Task<WorkflowRun?> FindAsync(string entityType, int entityId);
    Task UpdateStatusAsync(string temporalWorkflowId, WorkflowStatus status);
}