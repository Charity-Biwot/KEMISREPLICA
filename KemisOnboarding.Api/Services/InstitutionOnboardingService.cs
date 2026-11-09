using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using KemisOnboarding.Api.Data;
using KemisOnboarding.Api.Dtos;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Services;

public class OnboardingException : Exception
{
    public int StatusCode { get; }
    public object? Details { get; }

    public OnboardingException(int statusCode, string message, object? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}

public class InstitutionOnboardingService
{
    private static readonly string[] AllowedCertificateExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxCertificateBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public InstitutionOnboardingService(ApplicationDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }


    public async Task<CreateDraftResponse> CreateDraftAsync(InstitutionApplicationDto dto)
    {
        await EnsureUicIsUniqueAsync(dto.Uic, null);

        var resumeCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));

        var institution = new Institution { ResumeCodeHash = Hash(resumeCode) };
        Apply(institution, dto);

        _db.Institutions.Add(institution);
        await _db.SaveChangesAsync();

        return new CreateDraftResponse { Id = institution.Id, ResumeCode = resumeCode };
    }

    public async Task<InstitutionApplicationView> UpdateDraftAsync(int id, string resumeCode, InstitutionApplicationDto dto)
    {
        var institution = await GetOwnedAsync(id, resumeCode);
        EnsureDraft(institution);
        await EnsureUicIsUniqueAsync(dto.Uic, institution.Id);

        Apply(institution, dto);
        institution.UpdatedAt = DateTime.UtcNow;

        if (!dto.HasCertificate)
            DeleteCertificateFile(institution);

        await _db.SaveChangesAsync();
        return ToView(institution);
    }

    public async Task<InstitutionApplicationView> GetAsync(int id, string resumeCode)
    {
        var institution = await GetOwnedAsync(id, resumeCode);
        return ToView(institution);
    }


    public async Task UploadCertificateAsync(int id, string resumeCode, IFormFile file)
    {
        var institution = await GetOwnedAsync(id, resumeCode);
        EnsureDraft(institution);

        if (!institution.HasCertificate)
            throw new OnboardingException(400, "Set 'has certificate' to Yes before uploading a certificate.");

        if (file == null || file.Length == 0)
            throw new OnboardingException(400, "No file was uploaded.");

        if (file.Length > MaxCertificateBytes)
            throw new OnboardingException(400, "The certificate file must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedCertificateExtensions.Contains(extension))
            throw new OnboardingException(400, "Only PDF, JPG or PNG files are allowed.");

        var folder = Path.Combine(_env.ContentRootPath, "uploads", "certificates");
        Directory.CreateDirectory(folder);

        DeleteCertificateFile(institution);

        var fileName = $"{institution.Id}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        institution.CertificateFilePath = fullPath;
        institution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }


    public async Task<InstitutionApplicationView> SubmitAsync(int id, string resumeCode, SubmitRequest request)
    {
        var institution = await GetOwnedAsync(id, resumeCode);
        EnsureDraft(institution);

        if (institution.HasCertificate && string.IsNullOrEmpty(institution.CertificateFilePath))
            throw new OnboardingException(400, "Upload the registration certificate, or change the answer to No.");

        await EnsureUicIsUniqueAsync(institution.Uic, institution.Id);

        var matches = await FindPossibleDuplicatesAsync(institution);
        if (matches.Count > 0 && !request.AcknowledgeDuplicateWarning)
        {
            throw new OnboardingException(409,
                "Possible duplicate institutions were found. Review them and confirm to continue.", matches);
        }

        institution.Status = OnboardingStatus.Submitted;
        institution.SubmittedAt = DateTime.UtcNow;
        institution.ApplicationFee = _config.GetValue<decimal>("Onboarding:ApplicationFee", 0m);
        institution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToView(institution);
    }


    public async Task<InstitutionApplicationView> ConfirmPaymentAsync(int id, string resumeCode)
    {
        var institution = await GetOwnedAsync(id, resumeCode);

        if (institution.Status == OnboardingStatus.InApproval)
            return ToView(institution); // already paid, so calling it twice is safe

        if (institution.Status != OnboardingStatus.Submitted)
            throw new OnboardingException(409, "Submit the application before paying.");

        institution.PaymentReference = institution.ApplicationFee == 0
            ? "NO-FEE"
            : "MOCK-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        institution.PaidAt = DateTime.UtcNow;

        institution.Status = OnboardingStatus.InApproval;
        institution.ApprovalStartedAt = DateTime.UtcNow;
        institution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToView(institution);
    }


    private async Task<Institution> GetOwnedAsync(int id, string resumeCode)
    {
        var institution = await _db.Institutions.FirstOrDefaultAsync(i => i.Id == id);
        if (institution == null || string.IsNullOrWhiteSpace(resumeCode) ||
            institution.ResumeCodeHash != Hash(resumeCode))
        {
            throw new OnboardingException(404, "Application not found.");
        }
        return institution;
    }

    private static void EnsureDraft(Institution institution)
    {
        if (institution.Status != OnboardingStatus.Draft)
            throw new OnboardingException(409, "This application has already been submitted and can no longer be edited.");
    }

    private async Task EnsureUicIsUniqueAsync(string uic, int? exceptId)
    {
        var normalized = uic.Trim().ToUpperInvariant();
        var exists = await _db.Institutions.AnyAsync(i => i.Uic == normalized && i.Id != exceptId);
        if (exists)
            throw new OnboardingException(409, "This institutional code is already in use. Enter a unique code.");
    }

    private async Task<List<DuplicateMatchDto>> FindPossibleDuplicatesAsync(Institution current)
    {
        var subCounty = current.SubCounty.Trim().ToLower();

        var candidates = await _db.Institutions
            .Where(i => i.Id != current.Id && i.Status != OnboardingStatus.Draft)
            .Where(i =>
                (i.NormalizedName == current.NormalizedName && i.Level == current.Level &&
                 i.SubCounty.ToLower() == subCounty)
                ||
                (current.NormalizedCertificateNumber != null &&
                 i.NormalizedCertificateNumber == current.NormalizedCertificateNumber))
            .ToListAsync();

        return candidates.Select(i =>
        {
            var sameCertificate = current.NormalizedCertificateNumber != null &&
                                  i.NormalizedCertificateNumber == current.NormalizedCertificateNumber;
            return new DuplicateMatchDto
            {
                InstitutionId = i.Id,
                Name = i.Name,
                Uic = i.Uic,
                County = i.County,
                SubCounty = i.SubCounty,
                Level = i.Level.ToString(),
                CertificateNumber = i.CertificateNumber,
                Reason = sameCertificate
                    ? "Same registration certificate number"
                    : "Same name, level and sub-county"
            };
        }).ToList();
    }

    private static void Apply(Institution i, InstitutionApplicationDto dto)
    {
        i.StateDepartment = dto.StateDepartment;
        i.InstitutionType = dto.InstitutionType.Trim();
        i.ApplicationType = dto.ApplicationType;

        i.Name = dto.Name.Trim();
        i.NormalizedName = Normalize(dto.Name);
        i.Uic = dto.Uic.Trim().ToUpperInvariant();

        i.Email = dto.Email.Trim().ToLowerInvariant();
        i.MobileNumber = dto.MobileNumber.Trim();
        i.PostalCode = dto.PostalCode?.Trim();
        i.PostalLocation = dto.PostalLocation?.Trim();

        i.County = dto.County.Trim();
        i.SubCounty = dto.SubCounty.Trim();
        i.Constituency = dto.Constituency.Trim();
        i.Latitude = dto.Latitude;
        i.Longitude = dto.Longitude;

        i.Level = dto.Level;
        i.InstitutionLevelName = dto.InstitutionLevelName.Trim();
        i.SchoolCategories = dto.SchoolCategories?.Trim();
        i.AccommodationType = dto.AccommodationType;

        i.HasCertificate = dto.HasCertificate;
        if (dto.HasCertificate)
        {
            i.CertificateNumber = dto.CertificateNumber!.Trim();
            i.NormalizedCertificateNumber = Normalize(dto.CertificateNumber);
            i.NameAsOnCertificate = dto.NameAsOnCertificate!.Trim();
        }
        else
        {
            i.CertificateNumber = null;
            i.NormalizedCertificateNumber = null;
            i.NameAsOnCertificate = null;
        }
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", "");

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));

    private static void DeleteCertificateFile(Institution institution)
    {
        if (!string.IsNullOrEmpty(institution.CertificateFilePath) && File.Exists(institution.CertificateFilePath))
            File.Delete(institution.CertificateFilePath);
        institution.CertificateFilePath = null;
    }

    private static InstitutionApplicationView ToView(Institution i) => new()
    {
        Id = i.Id,
        Status = i.Status,
        CertificateUploaded = !string.IsNullOrEmpty(i.CertificateFilePath),
        CreatedAt = i.CreatedAt,
        SubmittedAt = i.SubmittedAt,
        ApplicationFee = i.ApplicationFee,
        PaymentReference = i.PaymentReference,
        PaidAt = i.PaidAt,
        ApprovalStartedAt = i.ApprovalStartedAt,
        StateDepartment = i.StateDepartment,
        InstitutionType = i.InstitutionType,
        ApplicationType = i.ApplicationType,
        Name = i.Name,
        Uic = i.Uic,
        Email = i.Email,
        MobileNumber = i.MobileNumber,
        PostalCode = i.PostalCode,
        PostalLocation = i.PostalLocation,
        County = i.County,
        SubCounty = i.SubCounty,
        Constituency = i.Constituency,
        Latitude = i.Latitude,
        Longitude = i.Longitude,
        Level = i.Level,
        InstitutionLevelName = i.InstitutionLevelName,
        SchoolCategories = i.SchoolCategories,
        AccommodationType = i.AccommodationType,
        HasCertificate = i.HasCertificate,
        CertificateNumber = i.CertificateNumber,
        NameAsOnCertificate = i.NameAsOnCertificate
    };
}