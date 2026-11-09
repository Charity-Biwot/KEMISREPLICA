
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

    // View institutions assigned to this SCDE.
    [HttpGet("institutions")]
    public Task<List<PendingInstitutionDto>> Institutions(
        CancellationToken ct) =>
        _workflow.GetScdeAssignedAsync(UserId, ct);

    // View details of an assigned institution.
    [HttpGet("institutions/{id:int}")]
    public Task<InstitutionDetailDto> Detail(
        int id, CancellationToken ct) =>
        _workflow.GetScdeDetailAsync(id, UserId, ct);

    // View available QASOs.
    [HttpGet("qasos")]
    public Task<List<UserSummary>> Qasos(CancellationToken ct) =>
        _workflow.GetQasosAsync(UserId, ct);

    // Assign a QASO to an institution.
    [HttpPost("institutions/{id:int}/assign-qaso")]
    public Task<InstitutionDetailDto> AssignQaso(
        int id, AssignRequest req, CancellationToken ct) =>
        _workflow.AssignQasoAsync(id, UserId, req, ct);
}
