using Microsoft.AspNetCore.Mvc;
using KemisOnboarding.Api.Dtos;
using KemisOnboarding.Api.Services;

namespace KemisOnboarding.Api.Controllers;

[ApiController]
[Route("api/institution-onboarding")]
public class InstitutionOnboardingController : ControllerBase
{
    private const string ResumeHeader = "X-Resume-Code";

    private readonly InstitutionOnboardingService _service;

    public InstitutionOnboardingController(InstitutionOnboardingService service)
    {
        _service = service;
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] InstitutionApplicationDto dto) =>
        Run(async () => Ok(await _service.CreateDraftAsync(dto)));

    [HttpGet("{id:int}")]
    public Task<IActionResult> Get(int id, [FromHeader(Name = ResumeHeader)] string? resumeCode) =>
        Run(async () => Ok(await _service.GetAsync(id, resumeCode ?? "")));

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromHeader(Name = ResumeHeader)] string? resumeCode,
        [FromBody] InstitutionApplicationDto dto) =>
        Run(async () => Ok(await _service.UpdateDraftAsync(id, resumeCode ?? "", dto)));

    [HttpPost("{id:int}/certificate")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> UploadCertificate(int id, [FromHeader(Name = ResumeHeader)] string? resumeCode,
        IFormFile file) =>
        Run(async () =>
        {
            await _service.UploadCertificateAsync(id, resumeCode ?? "", file);
            return Ok(new { message = "Certificate uploaded." });
        });

    [HttpPost("{id:int}/submit")]
    public Task<IActionResult> Submit(int id, [FromHeader(Name = ResumeHeader)] string? resumeCode,
        [FromBody] SubmitRequest? request) =>
        Run(async () => Ok(await _service.SubmitAsync(id, resumeCode ?? "", request ?? new SubmitRequest())));

    [HttpPost("{id:int}/payment")]
    public Task<IActionResult> ConfirmPayment(int id, [FromHeader(Name = ResumeHeader)] string? resumeCode) =>
        Run(async () => Ok(await _service.ConfirmPaymentAsync(id, resumeCode ?? "")));

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (OnboardingException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, details = ex.Details });
        }
    }
}