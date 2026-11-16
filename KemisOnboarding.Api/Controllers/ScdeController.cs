
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KemisOnboarding.Api.Dto;
using KemisOnboarding.Api.Models;
using KemisOnboarding.Api.Services;

namespace KemisOnboarding.Api.Controllers;

[ApiController]
[Route("api/scde")]
[Authorize(Roles = AppRoles.SCDE)]
public class ScdeController : ControllerBase
{
    private readonly InstitutionWorkflowService _workflow;

    public ScdeController(InstitutionWorkflowService workflow)
    {
        _workflow = workflow;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue("sub")!);

    // GET: api/scde/institutions
    [HttpGet("institutions")]
    public async Task<IActionResult> Institutions(
        CancellationToken ct)
    {
        var result = await _workflow.GetScdeAssignedAsync(UserId, ct);
        return Ok(result);
    }

    // GET: api/scde/institutions/{id}
    [HttpGet("institutions/{id:int}")]
    public async Task<IActionResult> Detail(
        int id,
        CancellationToken ct)
    {
        var result = await _workflow.GetScdeDetailAsync(id, UserId, ct);
        return Ok(result);
    }

    // GET: api/scde/qasos
    [HttpGet("qasos")]
    public async Task<IActionResult> Qasos(
        CancellationToken ct)
    {
        var result = await _workflow.GetQasosAsync(UserId, ct);
        return Ok(result);
    }

    // POST: api/scde/institutions/{id}/assign-qaso
    [HttpPost("institutions/{id:int}/assign-qaso")]
    public async Task<IActionResult> AssignQaso(
        int id,
        [FromBody] AssignRequest req,
        CancellationToken ct)
    {
        var result = await _workflow.AssignQasoAsync(
            id,
            UserId,
            req,
            ct);

        return Ok(result);
    }

    // POST: api/scde/institutions/{id}/review-qaso-report
    [HttpPost("institutions/{id:int}/review-qaso-report")]
    public async Task<IActionResult> ReviewQasoReport(
        int id,
        [FromBody] CommentRequest req,
        CancellationToken ct)
    {
        var result = await _workflow.ReviewQasoReportAsync(
            id,
            UserId,
            req,
            ct);

        return Ok(result);
    }

    // POST: api/scde/institutions/{id}/reports
    [HttpPost("institutions/{id:int}/reports")]
    public async Task<IActionResult> SubmitReport(
        int id,
        [FromBody] SubmitReportRequest req,
        CancellationToken ct)
    {
        if (req.InstitutionId != id)
        {
            return BadRequest("Institution ID does not match.");
        }

        var result = await _workflow.SubmitReportAsync(
            UserId,
            AppRoles.SCDE,
            req,
            ct);

        return Ok(result);
    }
}
