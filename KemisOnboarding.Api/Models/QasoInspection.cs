
namespace KemisOnboarding.Api.Models;

public enum CertificateValidationStatus
{
    NotReviewed = 0,
    Valid = 1,
    Invalid = 2,
    Missing = 3
}

public enum PhysicalInspectionStatus
{
    NotRequired = 0,
    Required = 1,
    Scheduled = 2,
    Completed = 3
}

public class QasoInspection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Each QASO inspection belongs to one QASO assignment.
    public Guid AssignmentId { get; set; }
    public InstitutionAssignment Assignment { get; set; } = null!;

    public CertificateValidationStatus CertificateStatus { get; set; }
        = CertificateValidationStatus.NotReviewed;

    // Certificate verification checklist.
    // null = not checked, true = passed, false = failed.
    public bool? CertificateAuthenticityVerified { get; set; }

    public bool? CertificateIssuerVerified { get; set; }

    public bool? CertificateNotExpired { get; set; }

    public bool? CertificateComplete { get; set; }

    // Notes or justification for the certificate decision.
    public string? CertificateReviewNotes { get; set; }

    public DateTime? CertificateReviewedAt { get; set; }

    public PhysicalInspectionStatus PhysicalInspectionStatus { get; set; }
        = PhysicalInspectionStatus.Required;

    public DateTime? ScheduledAt { get; set; }

    public string? ScheduleNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InspectionEvidence> EvidenceFiles { get; set; }
        = new List<InspectionEvidence>();
}

public class InspectionEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QasoInspectionId { get; set; }
    public QasoInspection QasoInspection { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
