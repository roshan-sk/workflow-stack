using Workflow.Application.DTOs;

namespace Workflow.Application.Interfaces;

public interface IWorkflowService
{
    Task StartAsync(StartWorkflowRequest request);
    Task SignalAsync(SignalWorkflowRequest request);
}