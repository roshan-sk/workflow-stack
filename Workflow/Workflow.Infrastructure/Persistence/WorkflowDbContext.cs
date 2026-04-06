using Microsoft.EntityFrameworkCore;
using Workflow.Core.Entities;

namespace Workflow.Infrastructure.Persistence;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.TemporalWorkflowId).IsUnique();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.TemporalWorkflowId).HasMaxLength(300).IsRequired();
            e.Property(x => x.WorkflowType).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
        });
    }
}