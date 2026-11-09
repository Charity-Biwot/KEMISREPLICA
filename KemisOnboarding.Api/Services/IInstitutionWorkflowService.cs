using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Services;

public interface IInstitutionWorkflowService
{
    Task<IReadOnlyList<Guid>> GetPendingInstitutionIdsAsync(Guid cdeUserId, CancellationToken ct = default);

    Task<InstitutionAssignment> AssignAsync(Guid institutionId, Guid assigneeUserId, AssignmentRole role, Guid cdeUserId, CancellationToken ct = default);

    Task<IReadOnlyList<InspectionReport>> GetReportsAsync(Guid institutionId, CancellationToken ct = default);
    Task<ReviewComment> AddCommentAsync(Guid institutionId, Guid? reportId, Guid authorUserId, string text, CancellationToken ct = default);

    Task<bool> CanDecideAsync(Guid institutionId, CancellationToken ct = default);

    Task<Institution> DecideAsync(Guid institutionId, bool approve, string reason, Guid cdeUserId, CancellationToken ct = default);
}