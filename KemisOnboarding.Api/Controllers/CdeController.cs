
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KemisOnboarding.Api.Dto;
using KemisOnboarding.Api.Models;
using KemisOnboarding.Api.Services;

namespace KemisOnboarding.Api.Controllers;

[ApiController]
[Route("api/cde")]
[Authorize(Roles = AppRoles.CDE)]
public class CdeController : ControllerBase
{
    private readonly InstitutionWorkflowService _workflow;

    public CdeController(InstitutionWorkflowService workflow)
    {
        _workflow = workflow;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue("sub")!);

    // CDE dashboard summary
    [HttpGet("dashboard")]
    public Task<CdeDashboardSummaryDto> Dashboard(
        CancellationToken ct) =>
        _workflow.GetDashboardSummaryAsync(UserId, ct);

    // Pending institutions
    [HttpGet("institutions/pending")]
    public Task<List<PendingInstitutionDto>> Pending(
        CancellationToken ct) =>
        _workflow.GetPendingAsync(UserId, ct);

    // Institution details
    [HttpGet("institutions/{id:int}")]
    public Task<InstitutionDetailDto> Detail(
        int id, CancellationToken ct) =>
        _workflow.GetDetailAsync(id, UserId, ct);

    // Available assignees
    [HttpGet("assignees")]
    public Task<List<UserSummary>> Assignees(
        [FromQuery] AssignmentRole role,
        CancellationToken ct) =>
        _workflow.GetAssigneesAsync(UserId, role, ct);

    // Assign an officer
    [HttpPost("institutions/{id:int}/assign")]
    public async Task<IActionResult> Assign(
        int id, AssignRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _workflow.AssignAsync(
                id, UserId, req, ct);

            return Ok(result);
        }
        catch (WorkflowException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
        }
    }

    // Add a comment
    [HttpPost("institutions/{id:int}/comments")]
    public Task<InstitutionDetailDto> Comment(
        int id, CommentRequest req, CancellationToken ct) =>
        _workflow.AddCommentAsync(id, UserId, req, ct);

    // Approve or reject an institution
    [HttpPost("institutions/{id:int}/decision")]
    public Task<InstitutionDetailDto> Decide(
        int id, DecisionRequest req, CancellationToken ct) =>
        _workflow.DecideAsync(id, UserId, req, ct);
}
