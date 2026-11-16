
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Dto;

// One row on the QASO dashboard.
public record QasoDashboardItemDto(
    Guid AssignmentId,
    int InstitutionId,
    string InstitutionName,
    string Uic,
    string County,
    string SubCounty,
    bool HasCertificate,
    string? CertificateNumber,
    DateTime AssignedAt,
    CertificateValidationStatus CertificateStatus,
    PhysicalInspectionStatus PhysicalInspectionStatus,
    DateTime? ScheduledAt,
    bool HasSubmittedReport
);

// Detailed information displayed when QASO opens an assigned institution.
public record QasoInspectionDetailDto(
    Guid AssignmentId,
    int InstitutionId,

    // Institution registration details submitted by HOI.
    string InstitutionName,
    string Uic,
    string Email,
    string MobileNumber,
    string County,
    string SubCounty,
    string InstitutionType,
    string InstitutionLevel,
    string? NameAsOnCertificate,
    string StateDepartment,
    string ApplicationType,
    string Constituency,
    double? Latitude,
    double? Longitude,
    string? PostalCode,
    string? PostalLocation,
    string? SchoolCategories,
    string AccommodationType,
    DateTime CreatedAt,
    DateTime? SubmittedAt,

    // Certificate information.
    bool HasCertificate,
    string? CertificateNumber,
    bool CertificateFileAvailable,
    CertificateValidationStatus CertificateStatus,

    // Certificate verification checklist.
    bool? CertificateAuthenticityVerified,
    bool? CertificateIssuerVerified,
    bool? CertificateNotExpired,
    bool? CertificateComplete,
    string? CertificateReviewNotes,
    DateTime? CertificateReviewedAt,

    // Physical inspection information.
    PhysicalInspectionStatus PhysicalInspectionStatus,
    DateTime? ScheduledAt,
    string? ScheduleNotes,

    // Report and evidence information.
    bool HasSubmittedReport,
    IReadOnlyList<InspectionEvidenceDto> EvidenceFiles
);

// QASO submits the four certificate verification checks.
public record ReviewCertificateRequest(
    bool? AuthenticityVerified,
    bool? IssuerVerified,
    bool? NotExpired,
    bool? Complete,
    string? Notes
);

// QASO submits this when scheduling a physical inspection.
public record ScheduleQasoInspectionRequest(
    DateTime ScheduledAt,
    string? Notes
);

// Safe evidence information returned to the frontend.
public record InspectionEvidenceDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
