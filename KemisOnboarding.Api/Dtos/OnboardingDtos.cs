using System.ComponentModel.DataAnnotations;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Dtos;

public class InstitutionApplicationDto : IValidatableObject
{
    public StateDepartment StateDepartment { get; set; }

    [Required, AllowedValues("Boys school", "Girls school", "Mixed",
        ErrorMessage = "Select a valid institution type.")]
    public string InstitutionType { get; set; } = "";

    public ApplicationType ApplicationType { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Required, StringLength(50)]
    public string Uic { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(@"^(?:\+254|0)[17]\d{8}$",
        ErrorMessage = "Enter a valid Kenyan mobile number, e.g. 0712345678 or +254712345678.")]
    public string MobileNumber { get; set; } = "";

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(200)]
    public string? PostalLocation { get; set; }

    [Required, StringLength(100)]
    public string County { get; set; } = "";

    [Required, StringLength(100)]
    public string SubCounty { get; set; } = "";

    [Required, StringLength(100)]
    public string Constituency { get; set; } = "";

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public InstitutionLevel Level { get; set; }

    [Required, StringLength(100)]
    public string InstitutionLevelName { get; set; } = "";

    [StringLength(200)]
    public string? SchoolCategories { get; set; }

    public AccommodationType AccommodationType { get; set; }

    public bool HasCertificate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(200)]
    public string? NameAsOnCertificate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        // Enums start at 1, so an unset (0) value is not defined.
        if (!Enum.IsDefined(StateDepartment))
            yield return new ValidationResult("Select the state department.", new[] { nameof(StateDepartment) });
        if (!Enum.IsDefined(ApplicationType))
            yield return new ValidationResult("Select the application type.", new[] { nameof(ApplicationType) });
        if (!Enum.IsDefined(Level))
            yield return new ValidationResult("Select the level.", new[] { nameof(Level) });
        if (!Enum.IsDefined(AccommodationType))
            yield return new ValidationResult("Select the accommodation type.", new[] { nameof(AccommodationType) });

        if (HasCertificate)
        {
            if (string.IsNullOrWhiteSpace(CertificateNumber))
                yield return new ValidationResult("Enter the registration certificate number.", new[] { nameof(CertificateNumber) });
            if (string.IsNullOrWhiteSpace(NameAsOnCertificate))
                yield return new ValidationResult("Enter the institution name as it appears on the certificate.", new[] { nameof(NameAsOnCertificate) });
        }

        if (Latitude.HasValue != Longitude.HasValue)
            yield return new ValidationResult("Provide both latitude and longitude, or neither.", new[] { nameof(Latitude), nameof(Longitude) });
    }
}

public class InstitutionApplicationView : InstitutionApplicationDto
{
    public int Id { get; set; }
    public OnboardingStatus Status { get; set; }
    public bool CertificateUploaded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal ApplicationFee { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ApprovalStartedAt { get; set; }
}

public class CreateDraftResponse
{
    public int Id { get; set; }

    public string ResumeCode { get; set; } = "";
}

public class SubmitRequest
{
    public bool AcknowledgeDuplicateWarning { get; set; }
}

public class DuplicateMatchDto
{
    public int InstitutionId { get; set; }
    public string Name { get; set; } = "";
    public string Uic { get; set; } = "";
    public string County { get; set; } = "";
    public string SubCounty { get; set; } = "";
    public string Level { get; set; } = "";
    public string? CertificateNumber { get; set; }
    public string Reason { get; set; } = "";
}