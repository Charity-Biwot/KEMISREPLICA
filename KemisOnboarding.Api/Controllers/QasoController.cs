
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KemisOnboarding.Api.Dto;
using KemisOnboarding.Api.Models;
using KemisOnboarding.Api.Services;

namespace KemisOnboarding.Api.Controllers;

[ApiController]
[Route("api/qaso")]
[Authorize(Roles = AppRoles.QASO)]
public class QasoController : ControllerBase
{
    private readonly InstitutionWorkflowService _workflow;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

    public QasoController(
        InstitutionWorkflowService workflow,
        IWebHostEnvironment environment)
    {
        _workflow = workflow;
        _environment = environment;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue("sub")!);

    // GET /api/qaso/institutions
    [HttpGet("institutions")]
    public async Task<IActionResult> GetAssignedInstitutions(
        CancellationToken ct)
    {
        var result = await _workflow.GetQasoAssignedAsync(
            UserId,
            ct);

        return Ok(result);
    }

    // GET /api/qaso/institutions/{id}
    [HttpGet("institutions/{id:int}")]
    public async Task<IActionResult> GetInstitutionDetails(
        int id,
        CancellationToken ct)
    {
        var result = await _workflow.GetQasoDetailAsync(
            id,
            UserId,
            ct);

        return Ok(result);
    }

    // POST /api/qaso/institutions/{id}/certificate-review
    [HttpPost("institutions/{id:int}/certificate-review")]
    public async Task<IActionResult> ReviewCertificate(
        int id,
        [FromBody] ReviewCertificateRequest request,
        CancellationToken ct)
    {
        var result = await _workflow.ReviewCertificateAsync(
            id,
            UserId,
            request,
            ct);

        return Ok(result);
    }

    // POST /api/qaso/institutions/{id}/schedule-inspection
    [HttpPost("institutions/{id:int}/schedule-inspection")]
    public async Task<IActionResult> SchedulePhysicalInspection(
        int id,
        [FromBody] ScheduleQasoInspectionRequest request,
        CancellationToken ct)
    {
        var result = await _workflow.SchedulePhysicalInspectionAsync(
            id,
            UserId,
            request,
            ct);

        return Ok(result);
    }

    // POST /api/qaso/institutions/{id}/complete-inspection
    [HttpPost("institutions/{id:int}/complete-inspection")]
    public async Task<IActionResult> CompletePhysicalInspection(
        int id,
        CancellationToken ct)
    {
        var result = await _workflow.CompletePhysicalInspectionAsync(
            id,
            UserId,
            ct);

        return Ok(result);
    }

    // POST /api/qaso/institutions/{id}/evidence
    [HttpPost("institutions/{id:int}/evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadInspectionEvidence(
        int id,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a file to upload."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "The maximum allowed file size is 5 MB."
            });
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = "Only PDF, JPG, JPEG and PNG files are allowed."
            });
        }

        var uploadDirectory = Path.Combine(
            _environment.ContentRootPath,
            "uploads",
            "inspection-evidence");

        Directory.CreateDirectory(uploadDirectory);

        var storedFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var filePath = Path.Combine(
            uploadDirectory,
            storedFileName);

        var originalFileName = Path.GetFileName(file.FileName);

        try
        {
            await using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await file.CopyToAsync(stream, ct);
            }

            var result = await _workflow.AddInspectionEvidenceAsync(
                id,
                UserId,
                originalFileName,
                storedFileName,
                filePath,
                file.ContentType,
                file.Length,
                ct);

            return Ok(result);
        }
        catch
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            throw;
        }
    }

    // POST /api/qaso/institutions/{id}/reports
    [HttpPost("institutions/{id:int}/reports")]
    public async Task<IActionResult> SubmitInspectionReport(
        int id,
        [FromBody] SubmitReportRequest request,
        CancellationToken ct)
    {
        if (id != request.InstitutionId)
        {
            return BadRequest(new
            {
                message = "The institution ID in the URL does not match the report request."
            });
        }

        var result = await _workflow.SubmitReportAsync(
            UserId,
            AppRoles.QASO,
            request,
            ct);

        return Ok(result);
    }
}
