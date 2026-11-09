namespace KemisOnboarding.Api.Models;

public enum StateDepartment { BasicEducation = 1, Tvet = 2, HigherEducation = 3 }

public enum ApplicationType { Regular = 1, Integrated = 2, SpecialSchool = 3, RegularWithSpecialUnit = 4 }

public enum AccommodationType { Day = 1, Boarding = 2, Hybrid = 3 }

public enum InstitutionLevel
{
    PrePrimary = 1,
    Primary = 2,
    PrePrimaryAndPrimary = 3,
    JuniorSchool = 4,
    PrimaryAndJunior = 5,
    SeniorSchool = 6,
    Pte = 7
}

public enum OnboardingStatus { Draft = 1, Submitted = 2, InApproval = 3, Approved = 4, Rejected = 5 }

public class Institution
{
    public int Id { get; set; }

    public string ResumeCodeHash { get; set; } = "";

    public StateDepartment StateDepartment { get; set; }

    public string InstitutionType { get; set; } = "";

    public ApplicationType ApplicationType { get; set; }

    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string? NameAsOnCertificate { get; set; }

    public string Uic { get; set; } = "";

    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string? PostalCode { get; set; }
    public string? PostalLocation { get; set; }

    public string County { get; set; } = "";
    public string SubCounty { get; set; } = "";
    public string Constituency { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public InstitutionLevel Level { get; set; }

    public string InstitutionLevelName { get; set; } = "";

    public string? SchoolCategories { get; set; }
    public AccommodationType AccommodationType { get; set; }

    public bool HasCertificate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? NormalizedCertificateNumber { get; set; }
    public string? CertificateFilePath { get; set; }

    public OnboardingStatus Status { get; set; } = OnboardingStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public decimal ApplicationFee { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ApprovalStartedAt { get; set; }

    // CDE decision
    public string? DecisionReason { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
}