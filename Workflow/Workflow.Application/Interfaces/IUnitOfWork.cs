namespace Workflow.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}