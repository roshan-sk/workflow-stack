using Microsoft.EntityFrameworkCore;
using Workflow.Application.Interfaces;
using Workflow.Core.Entities;
using Workflow.Core.Enums;

namespace Workflow.Infrastructure.Persistence;

public class WorkflowRunRepository : IWorkflowRunRepository
{
    private readonly WorkflowDbContext _db;

    public WorkflowRunRepository(WorkflowDbContext db) => _db = db;

    public async Task AddAsync(WorkflowRun run)
    {
        await _db.WorkflowRuns.AddAsync(run);
        await _db.SaveChangesAsync();
    }

    public Task<WorkflowRun?> FindAsync(string entityType, int entityId)
        => _db.WorkflowRuns
              .Where(r => r.EntityType == entityType && r.EntityId == entityId)
              .OrderByDescending(r => r.CreatedAt)
              .FirstOrDefaultAsync();

    public async Task UpdateStatusAsync(string temporalWorkflowId, WorkflowStatus status)
    {
        var run = await _db.WorkflowRuns
                           .FirstOrDefaultAsync(r => r.TemporalWorkflowId == temporalWorkflowId);
        if (run is null) return;

        run.Status = status;
        if (status == WorkflowStatus.Completed || status == WorkflowStatus.Failed)
            run.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}