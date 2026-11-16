
namespace KemisOnboarding.Api.Dto;

public record CdeInstitutionListDto(
    int Id,
    string Name,
    string Uic,
    string County,
    string SubCounty,
    DateTime? ApprovalStartedAt,
    string Status,
    string? DecisionReason,
    DateTime? DecidedAt,
    string? ScdeName,
    string? CqasoName
);
