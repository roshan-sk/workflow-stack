using Microsoft.Extensions.Configuration;
using Temporalio.Client;
using Workflow.Application.DTOs;
using Workflow.Application.Interfaces;
using Workflow.Core.Entities;
using Workflow.Core.Enums;
using Workflow.Infrastructure.Temporal.Workflows;

namespace Workflow.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly ITemporalClient _temporal;
    private readonly IWorkflowRegistry _registry;
    private readonly IWorkflowRunRepository _repository;
    private readonly IConfiguration _config;

    public WorkflowService(
        ITemporalClient temporal,
        IWorkflowRegistry registry,
        IWorkflowRunRepository repository,
        IConfiguration config)
    {
        _temporal = temporal;
        _registry = registry;
        _repository = repository;
        _config = config;
    }

    public async Task StartAsync(StartWorkflowRequest request)
    {
        var entry = _registry.Resolve(request.EntityType);

        var workflowId = entry.WorkflowIdPattern
            .Replace("{entityId}", request.EntityId.ToString())
            .Replace("{tenantId}", request.TenantId.ToString());

        var input = BuildInput(request);
        try {
            
            var test = await _temporal.StartWorkflowAsync(
                entry.WorkflowType,
                new[] { (object)input },
                new WorkflowOptions
                {
                    Id = workflowId,
                    TaskQueue = entry.TaskQueue
                });
            Console.WriteLine($"Workflow started with TLS {test.Id}");
        } catch (Exception ex)
        {
            Console.WriteLine($"Workflow start failed: {ex.Message}");
            throw;
        }

        await _repository.AddAsync(new WorkflowRun
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            TenantId = request.TenantId,
            TemporalWorkflowId = workflowId,
            WorkflowType = entry.WorkflowType,
            Status = WorkflowStatus.Running
        });
    }
    

    public async Task SignalAsync(SignalWorkflowRequest request)
    {
        var run = await _repository.FindAsync(request.EntityType, request.EntityId)
                ?? throw new InvalidOperationException(
                    $"No running workflow for {request.EntityType} #{request.EntityId}");
        try
        {
            var handle = _temporal.GetWorkflowHandle(run.TemporalWorkflowId);

            var payload = request.Payload ?? new Dictionary<string, object>();

            await handle.SignalAsync(
                (GenericApprovalWorkflow wf) => wf.ReceiveSignalAsync(
                    request.SignalName,
                    payload
                )
            );
            
        } catch (Exception ex)
        {
            Console.WriteLine($"Workflow start failed: {ex.Message}");
            throw;
        }
    }

    private WorkflowInput BuildInput(StartWorkflowRequest req)
    {
        // Load steps from config � fully dynamic, no hardcoding
        var steps = new List<WorkflowStep>();

        var stepsSection = _config.GetSection($"WorkflowSteps:{req.EntityType}");

        foreach (var stepSection in stepsSection.GetChildren())
        {
            steps.Add(new WorkflowStep(
                ActivityKey: stepSection["ActivityKey"] ?? string.Empty,
                WaitForSignal: stepSection["WaitForSignal"],
                RejectionActivityKey: stepSection["RejectionActivityKey"],
                TimeoutMinutes: int.TryParse(stepSection["TimeoutMinutes"], out var t) ? t : 5,
                MaxRetries: int.TryParse(stepSection["MaxRetries"], out var r) ? r : 3,
                SignalTimeoutDays: int.TryParse(stepSection["SignalTimeoutDays"], out var d) ? d : 7
            ));
        }

        return new WorkflowInput(
            EntityId: req.EntityId,
            EntityType: req.EntityType,
            TenantId: req.TenantId,
            Steps: steps,
            ExtraPayload: req.Payload);
    }
}