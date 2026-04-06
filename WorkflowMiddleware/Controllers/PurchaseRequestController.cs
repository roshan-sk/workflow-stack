using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/purchase-request")]
[Authorize]
public class PurchaseRequestController : ControllerBase
{
    private readonly WorkflowServiceClient _workflowClient;

    public PurchaseRequestController(WorkflowServiceClient workflowClient)
    {
        _workflowClient = workflowClient;
    }

    // START WORKFLOW
    [HttpPost("start")]
    public async Task<IActionResult> Start(StartWorkflowDto request)
    {
        var token = Request.Headers["Authorization"].ToString();

        await _workflowClient.StartWorkflowAsync(request.EntityId, token);
        return Ok(new { message = "Purchase Request Workflow Started" });
    }

    // MANAGER DECISION
    [HttpPost("manager-decision")]
    public async Task<IActionResult> ManagerDecision(ManagerDecisionDto request)
    {
        var token = Request.Headers["Authorization"].ToString();

        await _workflowClient.SendSignalAsync(
            request.EntityId,
            "ManagerDecision",
            request.Decision,
            token
        );

        return Ok(new { message = "Manager Decision Sent" });
    }

    // FINANCE DECISION
    [HttpPost("finance-decision")]
    public async Task<IActionResult> FinanceDecision(FinanceDecisionDto request)
    {
        var token = Request.Headers["Authorization"].ToString();
        await _workflowClient.SendSignalAsync(
            request.EntityId,
            "FinanceDecision",
            request.Decision,
            token
        );

        return Ok(new { message = "Finance Decision Sent" });
    }

    // FINANCE DECISION
    [HttpPost("hr-decision")]
    // [AllowAnonymous]
    public async Task<IActionResult> HrDecision(HrDecisionDto request)
    {
        var token = Request.Headers["Authorization"].ToString();
        await _workflowClient.SendSignalAsync(
            request.EntityId,
            "HrDecision",
            request.Decision,
            token
        );

        return Ok(new { message = "Hr Decision Sent" });
    }
}