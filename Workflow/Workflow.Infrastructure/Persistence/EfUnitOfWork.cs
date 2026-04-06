using Workflow.Application.Interfaces;

namespace Workflow.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly WorkflowDbContext _db;
    public EfUnitOfWork(WorkflowDbContext db) => _db = db;
    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}