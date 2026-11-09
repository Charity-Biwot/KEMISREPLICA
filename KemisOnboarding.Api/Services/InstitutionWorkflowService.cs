
using Microsoft.EntityFrameworkCore;
using KemisOnboarding.Api.Data;
using KemisOnboarding.Api.Dto;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Services;

public class InstitutionWorkflowService
{
    private readonly ApplicationDbContext _db;

    public InstitutionWorkflowService(ApplicationDbContext db)
    {
        _db = db;
    }

    // =========================================================
    // CDE DASHBOARD
    // =========================================================

    public async Task<CdeDashboardSummaryDto> GetDashboardSummaryAsync(
        Guid cdeUserId,
        CancellationToken ct)
    {
        var county = await GetCdeCountyAsync(cdeUserId, ct);

        var institutions = _db.Institutions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(county))
            institutions = institutions.Where(i => i.County == county);

        var approved = await institutions.CountAsync(
            i => i.Status == OnboardingStatus.Approved, ct);

        var rejected = await institutions.CountAsync(
            i => i.Status == OnboardingStatus.Rejected, ct);

        var pending = await institutions.CountAsync(
            i => i.Status == OnboardingStatus.InApproval, ct);

        return new CdeDashboardSummaryDto(approved, rejected, pending);
    }

    public async Task<List<PendingInstitutionDto>> GetPendingAsync(
        Guid cdeUserId,
        CancellationToken ct)
    {
        var county = await GetCdeCountyAsync(cdeUserId, ct);

        var query = _db.Institutions
            .Where(i => i.Status == OnboardingStatus.InApproval);

        if (!string.IsNullOrWhiteSpace(county))
            query = query.Where(i => i.County == county);

        var institutions = await query
            .OrderByDescending(i => i.ApprovalStartedAt)
            .ToListAsync(ct);

        var result = new List<PendingInstitutionDto>();

        foreach (var institution in institutions)
        {
            result.Add(await ToPendingDtoAsync(institution, ct));
        }

        return result;
    }

    // =========================================================
    // INSTITUTION DETAILS
    // =========================================================

    public async Task<InstitutionDetailDto> GetDetailAsync(
        int institutionId,
        Guid cdeUserId,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);

        await EnsureInScopeAsync(institution, cdeUserId, ct);

        return await GetDetailCoreAsync(institution, ct);
    }

    public async Task<List<UserSummary>> GetAssigneesAsync(
        Guid cdeUserId,
        AssignmentRole role,
        CancellationToken ct)
    {
        if (role != AssignmentRole.SCDE &&
            role != AssignmentRole.CQASO)
        {
            throw new WorkflowException(
                "CDE can assign only SCDE or CQASO officers.");
        }

        var county = await GetCdeCountyAsync(cdeUserId, ct);

        var users = _db.Users.Where(u =>
            u.IsActive && u.Role == RoleFor(role));

        if (!string.IsNullOrWhiteSpace(county))
        {
            users = users.Where(u =>
                u.County == null || u.County == county);
        }

        return await users
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummary(
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.County))
            .ToListAsync(ct);
    }

    // =========================================================
    // CDE ASSIGNMENT
    // =========================================================

    public async Task<InstitutionDetailDto> AssignAsync(
        int institutionId,
        Guid cdeUserId,
        AssignRequest req,
        CancellationToken ct)
    {
        var inst = await GetInstitutionAsync(institutionId, ct);

        await EnsureInScopeAsync(inst, cdeUserId, ct);

        if (inst.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is not awaiting approval.");

        if (req.Role != AssignmentRole.SCDE &&
            req.Role != AssignmentRole.CQASO)
        {
            throw new WorkflowException(
                "CDE can assign only SCDE or CQASO officers.");
        }

        var assignee = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == req.AssigneeUserId && u.IsActive, ct)
            ?? throw new WorkflowException(
                "Selected user not found.", 404);

        if (assignee.Role != RoleFor(req.Role))
            throw new WorkflowException(
                $"The selected user is not a {req.Role}.");

        if (!string.IsNullOrWhiteSpace(assignee.County) &&
            !string.Equals(
                assignee.County,
                inst.County,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkflowException(
                "The selected officer does not belong to this institution's county.");
        }

        var existing = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.Role == req.Role &&
                a.Status != AssignmentStatus.Cancelled, ct);

        if (existing != null)
        {
            if (existing.AssignedToUserId == assignee.Id)
                throw new WorkflowException(
                    $"This {req.Role} is already assigned to this institution.",
                    409);

            if (existing.Status == AssignmentStatus.ReportSubmitted)
                throw new WorkflowException(
                    $"The assigned {req.Role} has already submitted a report, so the assignment cannot be changed.",
                    409);

            existing.Status = AssignmentStatus.Cancelled;
        }

        _db.InstitutionAssignments.Add(new InstitutionAssignment
        {
            InstitutionId = institutionId,
            AssignedToUserId = assignee.Id,
            AssignedByUserId = cdeUserId,
            Role = req.Role
        });

        _db.Notifications.Add(new Notification
        {
            RecipientUserId = assignee.Id,
            InstitutionId = institutionId,
            Type = NotificationType.AssignmentMade,
            Message = $"You have been assigned as {req.Role} for {inst.Name}."
        });

        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(institutionId, cdeUserId, ct);
    }

    // =========================================================
    // CDE COMMENTS
    // =========================================================

    public async Task<InstitutionDetailDto> AddCommentAsync(
        int institutionId,
        Guid cdeUserId,
        CommentRequest req,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);

        await EnsureInScopeAsync(institution, cdeUserId, ct);

        if (string.IsNullOrWhiteSpace(req.Text))
            throw new WorkflowException("Comment cannot be empty.");

        if (req.ReportId.HasValue)
        {
            var reportExists = await _db.InspectionReports.AnyAsync(r =>
                r.Id == req.ReportId.Value &&
                r.InstitutionId == institutionId, ct);

            if (!reportExists)
                throw new WorkflowException(
                    "The selected report was not found.", 404);
        }

        _db.ReviewComments.Add(new ReviewComment
        {
            InstitutionId = institutionId,
            ReportId = req.ReportId,
            AuthorUserId = cdeUserId,
            Text = req.Text.Trim()
        });

        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(institutionId, cdeUserId, ct);
    }

    // =========================================================
    // CDE APPROVAL / REJECTION
    // =========================================================

    public async Task<InstitutionDetailDto> DecideAsync(
        int institutionId,
        Guid cdeUserId,
        DecisionRequest req,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);

        await EnsureInScopeAsync(institution, cdeUserId, ct);

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is not awaiting approval.");

        if (!req.Approve && string.IsNullOrWhiteSpace(req.Reason))
            throw new WorkflowException(
                "A reason is required when rejecting an institution.");

        if (!await CanDecideCoreAsync(institutionId, ct))
            throw new WorkflowException(
                "The required inspection reports have not all been submitted.");

        institution.Status = req.Approve
            ? OnboardingStatus.Approved
            : OnboardingStatus.Rejected;

        institution.DecisionReason = req.Approve
            ? null
            : req.Reason!.Trim();

        institution.DecidedByUserId = cdeUserId;
        institution.DecidedAt = DateTime.UtcNow;
        institution.UpdatedAt = DateTime.UtcNow;

        var assignedUsers = await _db.InstitutionAssignments
            .Where(a =>
                a.InstitutionId == institutionId &&
                a.Status != AssignmentStatus.Cancelled)
            .Select(a => a.AssignedToUserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in assignedUsers)
        {
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = userId,
                InstitutionId = institutionId,
                Type = req.Approve
                    ? NotificationType.InstitutionApproved
                    : NotificationType.InstitutionRejected,
                Message = req.Approve
                    ? $"{institution.Name} has been approved."
                    : $"{institution.Name} has been rejected. Reason: {req.Reason}"
            });
        }

        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(institutionId, cdeUserId, ct);
    }

    // =========================================================
    // SCDE DASHBOARD
    // =========================================================

    public async Task<List<PendingInstitutionDto>> GetScdeAssignedAsync(
        Guid scdeUserId,
        CancellationToken ct)
    {
        var institutionIds = await _db.InstitutionAssignments
            .Where(a =>
                a.AssignedToUserId == scdeUserId &&
                a.Role == AssignmentRole.SCDE &&
                a.Status != AssignmentStatus.Cancelled)
            .Select(a => a.InstitutionId)
            .Distinct()
            .ToListAsync(ct);

        var institutions = await _db.Institutions
            .Where(i =>
                institutionIds.Contains(i.Id) &&
                i.Status == OnboardingStatus.InApproval)
            .OrderByDescending(i => i.ApprovalStartedAt)
            .ToListAsync(ct);

        var result = new List<PendingInstitutionDto>();

        foreach (var institution in institutions)
        {
            result.Add(await ToPendingDtoAsync(institution, ct));
        }

        return result;
    }

    public async Task<InstitutionDetailDto> GetScdeDetailAsync(
        int institutionId,
        Guid scdeUserId,
        CancellationToken ct)
    {
        var assigned = await _db.InstitutionAssignments.AnyAsync(a =>
            a.InstitutionId == institutionId &&
            a.AssignedToUserId == scdeUserId &&
            a.Role == AssignmentRole.SCDE &&
            a.Status != AssignmentStatus.Cancelled, ct);

        if (!assigned)
            throw new WorkflowException(
                "You are not assigned to this institution.", 403);

        var institution = await GetInstitutionAsync(institutionId, ct);

        return await GetDetailCoreAsync(institution, ct);
    }

    // =========================================================
    // SCDE: AVAILABLE QASO OFFICERS
    // =========================================================

    public async Task<List<UserSummary>> GetQasosAsync(
        Guid scdeUserId,
        CancellationToken ct)
    {
        var scde = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == scdeUserId && u.IsActive, ct)
            ?? throw new WorkflowException(
                "SCDE user not found.", 404);

        var users = _db.Users.Where(u =>
            u.IsActive && u.Role == AppRoles.QASO);

        if (!string.IsNullOrWhiteSpace(scde.County))
        {
            users = users.Where(u =>
                u.County == null || u.County == scde.County);
        }

        return await users
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummary(
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.County))
            .ToListAsync(ct);
    }

    // =========================================================
    // SCDE: ASSIGN QASO
    // =========================================================

    public async Task<InstitutionDetailDto> AssignQasoAsync(
        int institutionId,
        Guid scdeUserId,
        AssignRequest req,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is not awaiting approval.");

        var scdeAssignment = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == scdeUserId &&
                a.Role == AssignmentRole.SCDE &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to this institution.", 403);

        if (scdeAssignment.Status == AssignmentStatus.ReportSubmitted)
            throw new WorkflowException(
                "You have already submitted your report. QASO assignment is no longer available.",
                409);

        if (req.Role != AssignmentRole.QASO)
            throw new WorkflowException(
                "SCDE can assign only QASO officers.");

        var qaso = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == req.AssigneeUserId &&
                u.IsActive &&
                u.Role == AppRoles.QASO, ct)
            ?? throw new WorkflowException(
                "Selected QASO officer was not found.", 404);

        if (!string.IsNullOrWhiteSpace(qaso.County) &&
            !string.Equals(
                qaso.County,
                institution.County,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkflowException(
                "The selected QASO does not belong to this institution's county.");
        }

        var existing = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct);

        if (existing != null)
        {
            if (existing.AssignedToUserId == qaso.Id)
                throw new WorkflowException(
                    "This QASO is already assigned to this institution.",
                    409);

            if (existing.Status == AssignmentStatus.ReportSubmitted)
                throw new WorkflowException(
                    "The assigned QASO has already submitted a report.",
                    409);

            existing.Status = AssignmentStatus.Cancelled;
        }

        _db.InstitutionAssignments.Add(new InstitutionAssignment
        {
            InstitutionId = institutionId,
            AssignedToUserId = qaso.Id,
            AssignedByUserId = scdeUserId,
            Role = AssignmentRole.QASO
        });

        _db.Notifications.Add(new Notification
        {
            RecipientUserId = qaso.Id,
            InstitutionId = institutionId,
            Type = NotificationType.AssignmentMade,
            Message = $"You have been assigned as QASO for {institution.Name}."
        });

        await _db.SaveChangesAsync(ct);

        return await GetScdeDetailAsync(
            institutionId, scdeUserId, ct);
    }

    // =========================================================
    // SCDE / QASO / CQASO: SUBMIT INSPECTION REPORT
    // =========================================================

    public async Task<ReportDto> SubmitReportAsync(
        Guid authorUserId,
        string authorRole,
        SubmitReportRequest req,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(
            req.InstitutionId, ct);

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is not awaiting inspection.");

        AssignmentRole assignmentRole;
        ReportType reportType;

        switch (authorRole)
        {
            case AppRoles.SCDE:
                assignmentRole = AssignmentRole.SCDE;
                reportType = ReportType.ScdeRecommendation;
                break;

            case AppRoles.QASO:
                assignmentRole = AssignmentRole.QASO;
                reportType = ReportType.QasoInspection;
                break;

            case AppRoles.CQASO:
                assignmentRole = AssignmentRole.CQASO;
                reportType = ReportType.CqasoInspection;
                break;

            default:
                throw new WorkflowException(
                    "Your role is not allowed to submit inspection reports.",
                    403);
        }

        var assignment = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == req.InstitutionId &&
                a.AssignedToUserId == authorUserId &&
                a.Role == assignmentRole &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        if (assignment.Status == AssignmentStatus.ReportSubmitted)
            throw new WorkflowException(
                "You have already submitted a report for this assignment.",
                409);

        if (string.IsNullOrWhiteSpace(req.Summary))
            throw new WorkflowException(
                "Report summary cannot be empty.");

        var report = new InspectionReport
        {
            InstitutionId = req.InstitutionId,
            AssignmentId = assignment.Id,
            AuthorUserId = authorUserId,
            Type = reportType,
            Summary = req.Summary.Trim(),
            Recommendation = req.Recommendation
        };

        assignment.Status = AssignmentStatus.ReportSubmitted;

        _db.InspectionReports.Add(report);

        _db.Notifications.Add(new Notification
        {
            RecipientUserId = assignment.AssignedByUserId,
            InstitutionId = req.InstitutionId,
            Type = NotificationType.ReportSubmitted,
            Message = $"{authorRole} has submitted an inspection report for {institution.Name}."
        });

        await _db.SaveChangesAsync(ct);

        var author = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == authorUserId, ct);

        return new ReportDto(
            report.Id,
            report.Type,
            author?.FullName ?? "Unknown",
            report.Summary,
            report.Recommendation,
            report.SubmittedAt);
    }

    // =========================================================
    // DECISION READINESS
    // =========================================================

    private async Task<bool> CanDecideCoreAsync(
        int institutionId,
        CancellationToken ct)
    {
        var assignments = await _db.InstitutionAssignments
            .Where(a =>
                a.InstitutionId == institutionId &&
                a.Status != AssignmentStatus.Cancelled)
            .ToListAsync(ct);

        var scdeAssignment = assignments.FirstOrDefault(
            a => a.Role == AssignmentRole.SCDE);

        if (scdeAssignment == null ||
            scdeAssignment.Status != AssignmentStatus.ReportSubmitted)
        {
            return false;
        }

        // Every additional assigned officer must submit a report.
        return assignments.All(a =>
            a.Status == AssignmentStatus.ReportSubmitted);
    }

    // =========================================================
    // SHARED DETAIL MAPPING
    // =========================================================

    private async Task<InstitutionDetailDto> GetDetailCoreAsync(
        Institution institution,
        CancellationToken ct)
    {
        var assignments = await _db.InstitutionAssignments
            .Where(a => a.InstitutionId == institution.Id)
            .OrderBy(a => a.AssignedAt)
            .ToListAsync(ct);

        var assignmentDtos = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Id == assignment.AssignedToUserId, ct);

            assignmentDtos.Add(new AssignmentDto(
                assignment.Id,
                assignment.AssignedToUserId,
                user?.FullName ?? "Unknown",
                assignment.Role,
                assignment.Status,
                assignment.AssignedAt));
        }

        var reports = await _db.InspectionReports
            .Where(r => r.InstitutionId == institution.Id)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

        var reportDtos = new List<ReportDto>();

        foreach (var report in reports)
        {
            var author = await _db.Users.FirstOrDefaultAsync(
                u => u.Id == report.AuthorUserId, ct);

            reportDtos.Add(new ReportDto(
                report.Id,
                report.Type,
                author?.FullName ?? "Unknown",
                report.Summary,
                report.Recommendation,
                report.SubmittedAt));
        }

        var comments = await _db.ReviewComments
            .Where(c => c.InstitutionId == institution.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var commentDtos = new List<CommentDto>();

        foreach (var comment in comments)
        {
            var author = await _db.Users.FirstOrDefaultAsync(
                u => u.Id == comment.AuthorUserId, ct);

            commentDtos.Add(new CommentDto(
                comment.Id,
                comment.ReportId,
                author?.FullName ?? "Unknown",
                comment.Text,
                comment.CreatedAt));
        }

        return new InstitutionDetailDto(
            institution.Id,
            institution.Name,
            institution.NameAsOnCertificate,
            institution.Uic,
            institution.StateDepartment.ToString(),
            institution.InstitutionType,
            institution.ApplicationType.ToString(),
            institution.InstitutionLevelName,
            institution.Email,
            institution.MobileNumber,
            institution.County,
            institution.SubCounty,
            institution.Constituency,
            institution.Latitude,
            institution.Longitude,
            institution.HasCertificate,
            institution.CertificateNumber,
            institution.Status.ToString(),
            institution.ApprovalStartedAt,
            institution.DecisionReason,
            institution.DecidedAt,
            assignmentDtos,
            reportDtos,
            commentDtos,
            await CanDecideCoreAsync(institution.Id, ct));
    }

    private async Task<PendingInstitutionDto> ToPendingDtoAsync(
        Institution institution,
        CancellationToken ct)
    {
        var assignments = await _db.InstitutionAssignments
            .Where(a =>
                a.InstitutionId == institution.Id &&
                a.Status != AssignmentStatus.Cancelled)
            .ToListAsync(ct);

        var canDecide = await CanDecideCoreAsync(
            institution.Id, ct);

        return new PendingInstitutionDto(
            institution.Id,
            institution.Name,
            institution.Uic,
            institution.County,
            institution.SubCounty,
            institution.ApprovalStartedAt,
            assignments.Any(a => a.Role == AssignmentRole.SCDE),
            assignments.Any(a => a.Role == AssignmentRole.CQASO),
            canDecide);
    }

    // =========================================================
    // SHARED HELPERS
    // =========================================================

    private async Task<Institution> GetInstitutionAsync(
        int institutionId,
        CancellationToken ct)
    {
        return await _db.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId, ct)
            ?? throw new WorkflowException(
                "Institution not found.", 404);
    }

    private async Task<string?> GetCdeCountyAsync(
        Guid cdeUserId,
        CancellationToken ct)
    {
        var cde = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == cdeUserId && u.IsActive, ct)
            ?? throw new WorkflowException(
                "CDE user not found.", 404);

        return cde.County;
    }

    private async Task EnsureInScopeAsync(
        Institution institution,
        Guid cdeUserId,
        CancellationToken ct)
    {
        var county = await GetCdeCountyAsync(cdeUserId, ct);

        if (!string.IsNullOrWhiteSpace(county) &&
            !string.Equals(
                county,
                institution.County,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkflowException(
                "You can only access institutions in your county.",
                403);
        }
    }

    private static string RoleFor(AssignmentRole role)
    {
        return role switch
        {
            AssignmentRole.SCDE => AppRoles.SCDE,
            AssignmentRole.CQASO => AppRoles.CQASO,
            AssignmentRole.QASO => AppRoles.QASO,
            _ => throw new WorkflowException(
                "Invalid assignment role.")
        };
    }
}
