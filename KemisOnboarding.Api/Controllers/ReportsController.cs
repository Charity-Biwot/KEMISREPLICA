using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KemisOnboarding.Api.Dto;
using KemisOnboarding.Api.Models;
using KemisOnboarding.Api.Services;

namespace KemisOnboarding.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{AppRoles.SCDE},{AppRoles.QASO},{AppRoles.CQASO}")]
public class ReportsController : ControllerBase
{
    private readonly InstitutionWorkflowService _workflow;

    public ReportsController(InstitutionWorkflowService workflow)
    {
        _workflow = workflow;
    }

    [HttpPost]
    public Task<ReportDto> Submit(SubmitReportRequest req, CancellationToken ct) =>
        _workflow.SubmitReportAsync(
            Guid.Parse(User.FindFirstValue("sub")!),
            User.FindFirstValue("role")!,
            req, ct);
}