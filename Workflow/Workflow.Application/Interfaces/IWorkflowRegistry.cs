using Workflow.Application.DTOs;

namespace Workflow.Application.Interfaces;

public interface IWorkflowRegistry
{
    WorkflowRegistryEntry Resolve(string entityType);
}