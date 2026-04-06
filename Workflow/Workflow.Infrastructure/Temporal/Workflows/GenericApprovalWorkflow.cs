using Temporalio.Workflows;
using Workflow.Infrastructure.Temporal.Activities;


namespace Workflow.Infrastructure.Temporal.Workflows;

[Workflow("GenericApprovalWorkflow")]
public class GenericApprovalWorkflow
{
    private string? _signalValue;
    private string? _lastSignalName;
    private Dictionary<string, object>? _signalPayload;

    [WorkflowSignal]
    public Task ReceiveSignalAsync(string signalName, Dictionary<string, object>? payload = null)
    {
        _lastSignalName = signalName;
        _signalValue = payload?.TryGetValue("decision", out var d) == true
            ? d?.ToString()
            : signalName;
        _signalPayload = payload;

        return Task.CompletedTask;
    }


    [WorkflowRun]
    public async Task RunAsync(WorkflowInput input)
    {
        try
        {
            foreach (var step in input.Steps)
            {
                await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                    (GenericWorkflowActivities a) =>
                        a.ExecuteActivityAsync(step.ActivityKey, input.EntityId, step.Payload),
                    new ActivityOptions
                    {
                        StartToCloseTimeout = TimeSpan.FromMinutes(
                            step.TimeoutMinutes > 0 ? step.TimeoutMinutes : 5),
                        RetryPolicy = new Temporalio.Common.RetryPolicy
                        {
                            MaximumAttempts = step.MaxRetries > 0 ? step.MaxRetries : 3
                        }
                    });

                if (!string.IsNullOrEmpty(step.WaitForSignal))
                {
                    _signalValue = null;
                    _lastSignalName = null;

                    var received = await Temporalio.Workflows.Workflow.WaitConditionAsync(
                        () =>  _lastSignalName == step.WaitForSignal && _signalValue != null,
                        TimeSpan.FromDays(step.SignalTimeoutDays > 0 ? step.SignalTimeoutDays : 7));

                    if (!received || _signalValue == null)
                        throw new TimeoutException(
                            $"Timed out waiting for signal '{step.WaitForSignal}'");

                    if (_signalValue.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(step.RejectionActivityKey))
                        {
                            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                                (GenericWorkflowActivities a) =>
                                    a.ExecuteActivityAsync(
                                        step.RejectionActivityKey,
                                        input.EntityId,
                                        _signalPayload),
                                new ActivityOptions
                                {
                                    StartToCloseTimeout = TimeSpan.FromMinutes(5)
                                });
                            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                                (GenericWorkflowActivities a) =>
                                    a.MarkWorkflowRejectedAsync(
                                        Temporalio.Workflows.Workflow.Info.WorkflowId
                                    ),
                                new ActivityOptions
                                {
                                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                                });
                        }
                        return;
                    }
                    if (_signalValue.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // go to next step 
                    }
                    throw new Exception($"Unknown decision received: {_signalValue}");
                }
            }
            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (GenericWorkflowActivities a) =>
                    a.MarkWorkflowCompletedAsync(Temporalio.Workflows.Workflow.Info.WorkflowId),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });
        } 
        catch
        {
            await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (GenericWorkflowActivities a) =>
                    a.MarkWorkflowFailedAsync(
                        Temporalio.Workflows.Workflow.Info.WorkflowId),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });

            throw;    
        }
    }
}

public record WorkflowInput(
    int EntityId,
    string EntityType,
    int TenantId,
    List<WorkflowStep> Steps,
    Dictionary<string, object>? ExtraPayload = null);

public record WorkflowStep(
    string ActivityKey,
    string? WaitForSignal = null,
    string? RejectionActivityKey = null,
    int TimeoutMinutes = 5,
    int MaxRetries = 3,
    int SignalTimeoutDays = 7,
    Dictionary<string, object>? Payload = null);