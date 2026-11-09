using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Dto;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, Guid UserId, string FullName, string Role, string? County);

public record CreateUserRequest(string FullName, string Email, string Password, string Role, string? County);
public record UserSummary(Guid Id, string FullName, string Email, string Role, string? County);

public record AssignRequest(Guid AssigneeUserId, AssignmentRole Role);
public record CommentRequest(string Text, Guid? ReportId);
public record DecisionRequest(bool Approve, string? Reason);
public record SubmitReportRequest(int InstitutionId, string Summary, ReportRecommendation Recommendation);

public record PendingInstitutionDto(
    int Id, string Name, string Uic, string County, string SubCounty,
    DateTime? ApprovalStartedAt, bool ScdeAssigned, bool CqasoAssigned, bool CanDecide);

public record AssignmentDto(
    Guid Id, Guid AssignedToUserId, string AssignedToName,
    AssignmentRole Role, AssignmentStatus Status, DateTime AssignedAt);

public record ReportDto(
    Guid Id, ReportType Type, string AuthorName, string Summary,
    ReportRecommendation Recommendation, DateTime SubmittedAt);

public record CommentDto(Guid Id, Guid? ReportId, string AuthorName, string Text, DateTime CreatedAt);

public record InstitutionDetailDto(
    int Id, string Name, string? NameAsOnCertificate, string Uic,
    string StateDepartment, string InstitutionType, string ApplicationType, string InstitutionLevel,
    string Email, string MobileNumber, string County, string SubCounty, string Constituency,
    double? Latitude, double? Longitude, bool HasCertificate, string? CertificateNumber,
    string Status, DateTime? ApprovalStartedAt, string? DecisionReason, DateTime? DecidedAt,
    List<AssignmentDto> Assignments, List<ReportDto> Reports, List<CommentDto> Comments,
    bool CanDecide);