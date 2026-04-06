using Microsoft.Extensions.Options;
using Workflow.Application.DTOs;
using Workflow.Application.Interfaces;

namespace Workflow.Infrastructure.Services;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, WorkflowRegistryEntry> _entries;

    public WorkflowRegistry(IOptions<List<WorkflowRegistryEntry>> options)
    {
        _entries = options.Value.ToDictionary(
            e => e.EntityType,
            StringComparer.OrdinalIgnoreCase);
    }

    public WorkflowRegistryEntry Resolve(string entityType)
    {
        if (_entries.TryGetValue(entityType, out var entry))
            return entry;

        throw new InvalidOperationException(
            $"No workflow registered for '{entityType}'. Add it to WorkflowRegistry in appsettings.json.");
    }
}