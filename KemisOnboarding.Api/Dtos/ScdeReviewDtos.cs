namespace KemisOnboarding.Api.Dto;

public record ScdeReviewQasoReportRequest(
    bool IsAccepted,
    string? Comments
);