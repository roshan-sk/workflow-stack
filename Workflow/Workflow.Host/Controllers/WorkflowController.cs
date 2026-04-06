using Microsoft.AspNetCore.Mvc;
using Workflow.Application.DTOs;
using Workflow.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;

namespace Workflow.Host.Controllers;

[ApiController]
[Route("workflows")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IWorkflowService workflowService, ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartWorkflowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityType))
            return BadRequest("EntityType is required.");

        _logger.LogInformation("Starting workflow for {EntityType} #{EntityId}",
            request.EntityType, request.EntityId);

        await _workflowService.StartAsync(request);
        return Ok(new { message = $"Workflow started for {request.EntityType} #{request.EntityId}" });
    }

    [HttpPost("signal")]
    public async Task<IActionResult> Signal([FromBody] SignalWorkflowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityType) || string.IsNullOrWhiteSpace(request.SignalName))
            return BadRequest("EntityType and SignalName are required.");

        _logger.LogInformation("Signal '{Signal}' for {EntityType} #{EntityId}",
            request.SignalName, request.EntityType, request.EntityId);

        await _workflowService.SignalAsync(request);
        return Ok(new { message = $"Signal '{request.SignalName}' sent to {request.EntityType} #{request.EntityId}" });
    }
}