
using System.IO;
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
            result.Add(await ToPendingDtoAsync(institution, ct));

        return result;
    }

    // =========================================================
    // CDE APPROVED INSTITUTIONS
    // =========================================================

    public async Task<List<CdeInstitutionListDto>> GetApprovedAsync(
        Guid cdeUserId,
        CancellationToken ct)
    {
        var county = await GetCdeCountyAsync(cdeUserId, ct);

        var query = _db.Institutions
            .AsNoTracking()
            .Where(i => i.Status == OnboardingStatus.Approved);

        if (!string.IsNullOrWhiteSpace(county))
            query = query.Where(i => i.County == county);

        var institutions = await query
            .OrderByDescending(i => i.DecidedAt)
            .ToListAsync(ct);

        var result = new List<CdeInstitutionListDto>();

        foreach (var institution in institutions)
        {
            result.Add(
                await ToCdeInstitutionListDtoAsync(institution, ct));
        }

        return result;
    }

    // =========================================================
    // CDE REJECTED INSTITUTIONS
    // =========================================================

    public async Task<List<CdeInstitutionListDto>> GetRejectedAsync(
        Guid cdeUserId,
        CancellationToken ct)
    {
        var county = await GetCdeCountyAsync(cdeUserId, ct);

        var query = _db.Institutions
            .AsNoTracking()
            .Where(i => i.Status == OnboardingStatus.Rejected);

        if (!string.IsNullOrWhiteSpace(county))
            query = query.Where(i => i.County == county);

        var institutions = await query
            .OrderByDescending(i => i.DecidedAt)
            .ToListAsync(ct);

        var result = new List<CdeInstitutionListDto>();

        foreach (var institution in institutions)
        {
            result.Add(
                await ToCdeInstitutionListDtoAsync(institution, ct));
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
    // CDE ASSIGNMENT: SCDE / OPTIONAL CQASO
    // =========================================================

    public async Task<InstitutionDetailDto> AssignAsync(
        int institutionId,
        Guid cdeUserId,
        AssignRequest req,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);
        await EnsureInScopeAsync(institution, cdeUserId, ct);

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is not awaiting approval.");

        if (req.Role != AssignmentRole.SCDE &&
            req.Role != AssignmentRole.CQASO)
        {
            throw new WorkflowException(
                "CDE can assign only SCDE or CQASO officers.");
        }

        if (req.Role == AssignmentRole.CQASO &&
            !await HasCompletedQasoReportAsync(institutionId, ct))
        {
            throw new WorkflowException(
                "CQASO can only be assigned after the QASO inspection report has been submitted.");
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
                institution.County,
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

            if (req.Role == AssignmentRole.SCDE)
            {
                var activeQasoExists =
                    await _db.InstitutionAssignments.AnyAsync(a =>
                        a.InstitutionId == institutionId &&
                        a.Role == AssignmentRole.QASO &&
                        a.Status != AssignmentStatus.Cancelled, ct);

                if (activeQasoExists)
                    throw new WorkflowException(
                        "The SCDE cannot be replaced while an active QASO assignment exists. Cancel the QASO assignment first.",
                        409);
            }

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
            Message = $"You have been assigned as {req.Role} for {institution.Name}."
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
                "The required QASO inspection report and SCDE recommendation must be submitted. If CQASO has been assigned, the CQASO report must also be submitted.");

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
    // QASO DASHBOARD
    // =========================================================

    public async Task<List<QasoDashboardItemDto>> GetQasoAssignedAsync(
        Guid qasoUserId,
        CancellationToken ct)
    {
        var assignments = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .Where(a =>
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled &&
                a.Institution.Status == OnboardingStatus.InApproval)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

        var result = new List<QasoDashboardItemDto>();

        foreach (var assignment in assignments)
        {
            var institution = assignment.Institution;

            var inspection = await _db.QasoInspections
                .FirstOrDefaultAsync(
                    q => q.AssignmentId == assignment.Id, ct);

            var hasSubmittedReport =
                await _db.InspectionReports.AnyAsync(r =>
                    r.AssignmentId == assignment.Id &&
                    r.AuthorUserId == qasoUserId &&
                    r.Type == ReportType.QasoInspection, ct);

            result.Add(new QasoDashboardItemDto(
                assignment.Id,
                institution.Id,
                institution.Name,
                institution.Uic,
                institution.County,
                institution.SubCounty,
                institution.HasCertificate,
                institution.CertificateNumber,
                assignment.AssignedAt,
                inspection?.CertificateStatus
                    ?? CertificateValidationStatus.NotReviewed,
                inspection?.PhysicalInspectionStatus
                    ?? PhysicalInspectionStatus.Required,
                inspection?.ScheduledAt,
                hasSubmittedReport));
        }

        return result;
    }

    // =========================================================
    // QASO: INSTITUTION INSPECTION DETAILS
    // =========================================================

    public async Task<QasoInspectionDetailDto> GetQasoDetailAsync(
        int institutionId,
        Guid qasoUserId,
        CancellationToken ct)
    {
        var assignment = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        var institution = assignment.Institution;

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is no longer awaiting inspection.",
                409);

        var inspection = await _db.QasoInspections
            .Include(q => q.EvidenceFiles)
            .FirstOrDefaultAsync(
                q => q.AssignmentId == assignment.Id, ct);

        if (inspection == null)
        {
            inspection = new QasoInspection
            {
                AssignmentId = assignment.Id,
                CertificateStatus = CertificateValidationStatus.NotReviewed,
                PhysicalInspectionStatus = PhysicalInspectionStatus.Required,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.QasoInspections.Add(inspection);
            await _db.SaveChangesAsync(ct);

            inspection = await _db.QasoInspections
                .Include(q => q.EvidenceFiles)
                .FirstOrDefaultAsync(
                    q => q.AssignmentId == assignment.Id, ct)
                ?? throw new WorkflowException(
                    "Unable to create the QASO inspection record.", 500);
        }

        var hasSubmittedReport =
            await _db.InspectionReports.AnyAsync(r =>
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == qasoUserId &&
                r.Type == ReportType.QasoInspection, ct);

        var evidence = inspection.EvidenceFiles
            .OrderByDescending(e => e.UploadedAt)
            .Select(e => new InspectionEvidenceDto(
                e.Id,
                e.OriginalFileName,
                e.ContentType,
                e.FileSizeBytes,
                e.UploadedAt))
            .ToList();

        return new QasoInspectionDetailDto(
            assignment.Id,
            institution.Id,
            institution.Name,
            institution.Uic,
            institution.Email,
            institution.MobileNumber,
            institution.County,
            institution.SubCounty,
            institution.InstitutionType,
            institution.InstitutionLevelName,
            institution.NameAsOnCertificate,
            institution.StateDepartment.ToString(),
            institution.ApplicationType.ToString(),
            institution.Constituency,
            institution.Latitude,
            institution.Longitude,
            institution.PostalCode,
            institution.PostalLocation,
            institution.SchoolCategories,
            institution.AccommodationType.ToString(),
            institution.CreatedAt,
            institution.SubmittedAt,
            institution.HasCertificate,
            institution.CertificateNumber,
            !string.IsNullOrWhiteSpace(institution.CertificateFilePath)
                && File.Exists(institution.CertificateFilePath),
            inspection.CertificateStatus,
            inspection.CertificateAuthenticityVerified,
            inspection.CertificateIssuerVerified,
            inspection.CertificateNotExpired,
            inspection.CertificateComplete,
            inspection.CertificateReviewNotes,
            inspection.CertificateReviewedAt,
            inspection.PhysicalInspectionStatus,
            inspection.ScheduledAt,
            inspection.ScheduleNotes,
            hasSubmittedReport,
            evidence);
    }

    // =========================================================
    // QASO: REVIEW INSTITUTION CERTIFICATE
    // =========================================================

    public async Task<QasoInspectionDetailDto> ReviewCertificateAsync(
        int institutionId,
        Guid qasoUserId,
        ReviewCertificateRequest request,
        CancellationToken ct)
    {
        var assignment = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        if (assignment.Institution.Status != OnboardingStatus.InApproval)
        {
            throw new WorkflowException(
                "This institution is no longer awaiting inspection.",
                409);
        }

        var inspection = await GetOrCreateQasoInspectionAsync(
            assignment.Id, ct);

        var reportAlreadySubmitted =
            await _db.InspectionReports.AnyAsync(r =>
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == qasoUserId &&
                r.Type == ReportType.QasoInspection, ct);

        if (reportAlreadySubmitted)
        {
            throw new WorkflowException(
                "You have already submitted the QASO inspection report.",
                409);
        }

        var certificateFileAvailable =
            assignment.Institution.HasCertificate &&
            !string.IsNullOrWhiteSpace(
                assignment.Institution.CertificateFilePath) &&
            File.Exists(assignment.Institution.CertificateFilePath);

        if (!certificateFileAvailable)
        {
            inspection.CertificateStatus =
                CertificateValidationStatus.Missing;

            inspection.CertificateAuthenticityVerified = null;
            inspection.CertificateIssuerVerified = null;
            inspection.CertificateNotExpired = null;
            inspection.CertificateComplete = null;

            inspection.PhysicalInspectionStatus =
                PhysicalInspectionStatus.Required;

            inspection.CertificateReviewNotes =
                string.IsNullOrWhiteSpace(request.Notes)
                    ? "Certificate file is missing or unavailable."
                    : request.Notes.Trim();
        }
        else
        {
            if (request.AuthenticityVerified == null ||
                request.IssuerVerified == null ||
                request.NotExpired == null ||
                request.Complete == null)
            {
                throw new WorkflowException(
                    "Please complete all four certificate verification checks.",
                    400);
            }

            inspection.CertificateAuthenticityVerified =
                request.AuthenticityVerified;

            inspection.CertificateIssuerVerified =
                request.IssuerVerified;

            inspection.CertificateNotExpired =
                request.NotExpired;

            inspection.CertificateComplete =
                request.Complete;

            var certificateIsValid =
                request.AuthenticityVerified.Value &&
                request.IssuerVerified.Value &&
                request.NotExpired.Value &&
                request.Complete.Value;

            if (certificateIsValid)
            {
                inspection.CertificateStatus =
                    CertificateValidationStatus.Valid;

                inspection.PhysicalInspectionStatus =
                    PhysicalInspectionStatus.NotRequired;

                inspection.CertificateReviewNotes =
                    request.Notes?.Trim();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Notes))
                {
                    throw new WorkflowException(
                        "Please provide a justification explaining why the certificate failed verification.",
                        400);
                }

                inspection.CertificateStatus =
                    CertificateValidationStatus.Invalid;

                inspection.PhysicalInspectionStatus =
                    PhysicalInspectionStatus.Required;

                inspection.CertificateReviewNotes =
                    request.Notes.Trim();
            }
        }

        inspection.CertificateReviewedAt = DateTime.UtcNow;
        inspection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetQasoDetailAsync(
            institutionId,
            qasoUserId,
            ct);
    }

    // =========================================================
    // QASO: SCHEDULE PHYSICAL INSPECTION
    // =========================================================

    public async Task<QasoInspectionDetailDto> SchedulePhysicalInspectionAsync(
        int institutionId,
        Guid qasoUserId,
        ScheduleQasoInspectionRequest request,
        CancellationToken ct)
    {
        var assignment = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        if (assignment.Institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is no longer awaiting inspection.");

        var inspection = await GetOrCreateQasoInspectionAsync(
            assignment.Id, ct);

        var reportAlreadySubmitted =
            await _db.InspectionReports.AnyAsync(r =>
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == qasoUserId &&
                r.Type == ReportType.QasoInspection, ct);

        if (reportAlreadySubmitted)
            throw new WorkflowException(
                "You have already submitted the QASO inspection report.",
                409);

        if (inspection.CertificateStatus != CertificateValidationStatus.Missing &&
            inspection.CertificateStatus != CertificateValidationStatus.Invalid)
            throw new WorkflowException(
                "A physical inspection is only required when the certificate is missing or invalid.",
                400);

        if (inspection.PhysicalInspectionStatus != PhysicalInspectionStatus.Required)
            throw new WorkflowException(
                "This physical inspection has already been scheduled or completed.",
                409);

        if (request.ScheduledAt <= DateTime.UtcNow)
            throw new WorkflowException(
                "The physical inspection date must be in the future.",
                400);

        inspection.PhysicalInspectionStatus =
            PhysicalInspectionStatus.Scheduled;

        inspection.ScheduledAt = request.ScheduledAt;
        inspection.ScheduleNotes = request.Notes?.Trim();
        inspection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetQasoDetailAsync(institutionId, qasoUserId, ct);
    }

    // =========================================================
    // QASO: UPLOAD PHYSICAL INSPECTION EVIDENCE
    // =========================================================

    public async Task<QasoInspectionDetailDto> AddInspectionEvidenceAsync(
        int institutionId,
        Guid qasoUserId,
        string originalFileName,
        string storedFileName,
        string filePath,
        string contentType,
        long fileSizeBytes,
        CancellationToken ct)
    {
        var assignment = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        if (assignment.Institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is no longer awaiting inspection.",
                409);

        var inspection = await GetOrCreateQasoInspectionAsync(
            assignment.Id, ct);

        if (inspection.PhysicalInspectionStatus != PhysicalInspectionStatus.Scheduled)
            throw new WorkflowException(
                "Schedule the physical inspection before uploading evidence.",
                409);

        var reportAlreadySubmitted =
            await _db.InspectionReports.AnyAsync(r =>
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == qasoUserId &&
                r.Type == ReportType.QasoInspection, ct);

        if (reportAlreadySubmitted)
            throw new WorkflowException(
                "You have already submitted the QASO inspection report.",
                409);

        if (fileSizeBytes <= 0)
            throw new WorkflowException(
                "The uploaded file is empty.",
                400);

        _db.InspectionEvidence.Add(new InspectionEvidence
        {
            QasoInspectionId = inspection.Id,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = qasoUserId,
            UploadedAt = DateTime.UtcNow
        });

        inspection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetQasoDetailAsync(institutionId, qasoUserId, ct);
    }

    // =========================================================
    // QASO: COMPLETE PHYSICAL INSPECTION
    // =========================================================

    public async Task<QasoInspectionDetailDto> CompletePhysicalInspectionAsync(
        int institutionId,
        Guid qasoUserId,
        CancellationToken ct)
    {
        var assignment = await _db.InstitutionAssignments
            .Include(a => a.Institution)
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == qasoUserId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct)
            ?? throw new WorkflowException(
                "You are not assigned to inspect this institution.",
                403);

        if (assignment.Institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is no longer awaiting inspection.",
                409);

        var inspection = await GetOrCreateQasoInspectionAsync(
            assignment.Id, ct);

        if (inspection.PhysicalInspectionStatus != PhysicalInspectionStatus.Scheduled)
            throw new WorkflowException(
                "Only a scheduled physical inspection can be marked as completed.",
                409);

        var hasEvidence = await _db.InspectionEvidence.AnyAsync(
            e => e.QasoInspectionId == inspection.Id, ct);

        if (!hasEvidence)
            throw new WorkflowException(
                "Upload at least one inspection evidence file before completing the inspection.",
                400);

        var reportAlreadySubmitted =
            await _db.InspectionReports.AnyAsync(r =>
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == qasoUserId &&
                r.Type == ReportType.QasoInspection, ct);

        if (reportAlreadySubmitted)
            throw new WorkflowException(
                "The QASO inspection report has already been submitted.",
                409);

        inspection.PhysicalInspectionStatus =
            PhysicalInspectionStatus.Completed;

        inspection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetQasoDetailAsync(institutionId, qasoUserId, ct);
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
            result.Add(await ToPendingDtoAsync(institution, ct));

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

        return await GetDetailCoreAsync(
            institution, ct, AssignmentRole.QASO);
    }

    // =========================================================
    // SCDE: REVIEW QASO INSPECTION REPORT
    // =========================================================

    public async Task<InstitutionDetailDto> ReviewQasoReportAsync(
        int institutionId,
        Guid scdeUserId,
        CommentRequest request,
        CancellationToken ct)
    {
        var institution = await GetInstitutionAsync(institutionId, ct);

        if (institution.Status != OnboardingStatus.InApproval)
            throw new WorkflowException(
                "This institution is no longer awaiting review.", 409);

        var scdeAssignment = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.AssignedToUserId == scdeUserId &&
                a.Role == AssignmentRole.SCDE &&
                a.Status != AssignmentStatus.Cancelled, ct);

        if (scdeAssignment == null)
            throw new WorkflowException(
                "You are not assigned to this institution.", 403);

        if (scdeAssignment.Status == AssignmentStatus.ReportSubmitted)
            throw new WorkflowException(
                "You have already submitted your SCDE recommendation.", 409);

        if (string.IsNullOrWhiteSpace(request.Text))
            throw new WorkflowException(
                "Review comment cannot be empty.");

        var qasoAssignment = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct);

        if (qasoAssignment == null)
            throw new WorkflowException(
                "No active QASO assignment exists.");

        var qasoReport = await _db.InspectionReports
            .FirstOrDefaultAsync(r =>
                r.InstitutionId == institutionId &&
                r.AssignmentId == qasoAssignment.Id &&
                r.AuthorUserId == qasoAssignment.AssignedToUserId &&
                r.Type == ReportType.QasoInspection, ct);

        if (qasoReport == null ||
            qasoAssignment.Status != AssignmentStatus.ReportSubmitted)
            throw new WorkflowException(
                "The QASO inspection report has not been submitted.");

        if (request.ReportId.HasValue &&
            request.ReportId.Value != qasoReport.Id)
            throw new WorkflowException(
                "The selected report is not the active QASO inspection report.");

        _db.ReviewComments.Add(new ReviewComment
        {
            InstitutionId = institutionId,
            ReportId = qasoReport.Id,
            AuthorUserId = scdeUserId,
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return await GetScdeDetailAsync(institutionId, scdeUserId, ct);
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
            users = users.Where(u =>
                u.County == null || u.County == scde.County);

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
            throw new WorkflowException(
                "The selected QASO does not belong to this institution's county.");

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

        var qasoAssignment = new InstitutionAssignment
        {
            InstitutionId = institutionId,
            AssignedToUserId = qaso.Id,
            AssignedByUserId = scdeUserId,
            Role = AssignmentRole.QASO
        };

        _db.InstitutionAssignments.Add(qasoAssignment);

        await _db.SaveChangesAsync(ct);

        var qasoInspection = new QasoInspection
        {
            AssignmentId = qasoAssignment.Id,
            CertificateStatus = CertificateValidationStatus.NotReviewed,
            PhysicalInspectionStatus = PhysicalInspectionStatus.Required,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.QasoInspections.Add(qasoInspection);

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

        if (string.IsNullOrWhiteSpace(req.Summary))
            throw new WorkflowException(
                "Report summary cannot be empty.");

        if (!Enum.IsDefined(
            typeof(ReportRecommendation), req.Recommendation))
            throw new WorkflowException(
                "Invalid report recommendation.");

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

        var existingReport = await _db.InspectionReports.AnyAsync(r =>
            r.AssignmentId == assignment.Id, ct);

        if (existingReport)
            throw new WorkflowException(
                "A report has already been submitted for this assignment.",
                409);

        if (authorRole == AppRoles.QASO)
        {
            var activeScdeAssignment =
                await _db.InstitutionAssignments.FirstOrDefaultAsync(a =>
                    a.InstitutionId == req.InstitutionId &&
                    a.Role == AssignmentRole.SCDE &&
                    a.Status != AssignmentStatus.Cancelled, ct);

            if (activeScdeAssignment == null ||
                assignment.AssignedByUserId != activeScdeAssignment.AssignedToUserId)
                throw new WorkflowException(
                    "The QASO assignment is not linked to the active SCDE assignment.");
        }

        if (authorRole == AppRoles.SCDE &&
            !await HasCompletedQasoReportAsync(req.InstitutionId, ct))
            throw new WorkflowException(
                "The QASO inspection report must be submitted before the SCDE can submit a recommendation.");

        if (authorRole == AppRoles.CQASO &&
            !await HasCompletedQasoReportAsync(req.InstitutionId, ct))
            throw new WorkflowException(
                "The QASO inspection report must be submitted before CQASO can submit a second-level inspection report.");

        if (authorRole == AppRoles.QASO)
        {
            var qasoInspection = await GetOrCreateQasoInspectionAsync(
                assignment.Id, ct);

            var evidenceFiles = await _db.InspectionEvidence
                .Where(e => e.QasoInspectionId == qasoInspection.Id)
                .ToListAsync(ct);

            if (qasoInspection.CertificateStatus ==
                CertificateValidationStatus.NotReviewed)
                throw new WorkflowException(
                    "Review the institution certificate before submitting the QASO report.");

            if (qasoInspection.CertificateStatus ==
                    CertificateValidationStatus.Missing ||
                qasoInspection.CertificateStatus ==
                    CertificateValidationStatus.Invalid)
            {
                if (qasoInspection.PhysicalInspectionStatus !=
                    PhysicalInspectionStatus.Completed)
                    throw new WorkflowException(
                        "Complete the physical inspection before submitting the QASO report.");

                if (!evidenceFiles.Any())
                    throw new WorkflowException(
                        "Upload at least one physical inspection evidence file before submitting the QASO report.");
            }

            if (qasoInspection.CertificateStatus ==
                    CertificateValidationStatus.Valid &&
                qasoInspection.PhysicalInspectionStatus !=
                    PhysicalInspectionStatus.NotRequired)
                throw new WorkflowException(
                    "The physical inspection status is inconsistent with the valid certificate review.");
        }

        var report = new InspectionReport
        {
            InstitutionId = req.InstitutionId,
            AssignmentId = assignment.Id,
            AuthorUserId = authorUserId,
            Type = reportType,
            Summary = req.Summary.Trim(),
            Findings = string.IsNullOrWhiteSpace(req.Findings)
                ? null
                : req.Findings.Trim(),
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
            report.Findings,
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

        var scdeAssignments = assignments
            .Where(a => a.Role == AssignmentRole.SCDE).ToList();

        var qasoAssignments = assignments
            .Where(a => a.Role == AssignmentRole.QASO).ToList();

        var cqasoAssignments = assignments
            .Where(a => a.Role == AssignmentRole.CQASO).ToList();

        if (scdeAssignments.Count != 1 ||
            qasoAssignments.Count != 1 ||
            cqasoAssignments.Count > 1)
            return false;

        var scdeAssignment = scdeAssignments[0];
        var qasoAssignment = qasoAssignments[0];

        if (scdeAssignment.Status != AssignmentStatus.ReportSubmitted ||
            qasoAssignment.Status != AssignmentStatus.ReportSubmitted)
            return false;

        if (qasoAssignment.AssignedByUserId !=
            scdeAssignment.AssignedToUserId)
            return false;

        if (!await HasReportForAssignmentAsync(
            qasoAssignment, ReportType.QasoInspection, ct))
            return false;

        if (!await HasReportForAssignmentAsync(
            scdeAssignment, ReportType.ScdeRecommendation, ct))
            return false;

        if (cqasoAssignments.Count == 1)
        {
            var cqasoAssignment = cqasoAssignments[0];

            if (cqasoAssignment.Status != AssignmentStatus.ReportSubmitted)
                return false;

            if (!await HasReportForAssignmentAsync(
                cqasoAssignment, ReportType.CqasoInspection, ct))
                return false;
        }

        return true;
    }

    // =========================================================
    // REPORT VALIDATION HELPERS
    // =========================================================

    private async Task<bool> HasCompletedQasoReportAsync(
        int institutionId,
        CancellationToken ct)
    {
        var qasoAssignment = await _db.InstitutionAssignments
            .FirstOrDefaultAsync(a =>
                a.InstitutionId == institutionId &&
                a.Role == AssignmentRole.QASO &&
                a.Status != AssignmentStatus.Cancelled, ct);

        if (qasoAssignment == null ||
            qasoAssignment.Status != AssignmentStatus.ReportSubmitted)
            return false;

        return await HasReportForAssignmentAsync(
            qasoAssignment, ReportType.QasoInspection, ct);
    }

    private async Task<bool> HasReportForAssignmentAsync(
        InstitutionAssignment assignment,
        ReportType expectedType,
        CancellationToken ct)
    {
        var matchingReports = await _db.InspectionReports
            .CountAsync(r =>
                r.InstitutionId == assignment.InstitutionId &&
                r.AssignmentId == assignment.Id &&
                r.AuthorUserId == assignment.AssignedToUserId &&
                r.Type == expectedType, ct);

        return matchingReports == 1;
    }

    // =========================================================
    // QASO INSPECTION RECORD HELPER
    // =========================================================

    private async Task<QasoInspection> GetOrCreateQasoInspectionAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        var inspection = await _db.QasoInspections
            .FirstOrDefaultAsync(
                q => q.AssignmentId == assignmentId, ct);

        if (inspection != null)
            return inspection;

        inspection = new QasoInspection
        {
            AssignmentId = assignmentId,
            CertificateStatus = CertificateValidationStatus.NotReviewed,
            PhysicalInspectionStatus = PhysicalInspectionStatus.Required,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.QasoInspections.Add(inspection);
        await _db.SaveChangesAsync(ct);

        return inspection;
    }

    // =========================================================
    // SHARED DETAIL MAPPING
    // =========================================================

    private async Task<InstitutionDetailDto> GetDetailCoreAsync(
        Institution institution,
        CancellationToken ct,
        AssignmentRole? visibleAssignmentRole = null)
    {
        var assignmentsQuery = _db.InstitutionAssignments
            .Where(a => a.InstitutionId == institution.Id);

        if (visibleAssignmentRole.HasValue)
            assignmentsQuery = assignmentsQuery.Where(
                a => a.Role == visibleAssignmentRole.Value);

        var assignments = await assignmentsQuery
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

        var reportsQuery = _db.InspectionReports
            .Where(r => r.InstitutionId == institution.Id);

        if (visibleAssignmentRole == AssignmentRole.QASO)
        {
            reportsQuery = reportsQuery.Where(r =>
                r.Type == ReportType.QasoInspection ||
                r.Type == ReportType.ScdeRecommendation);
        }

        var reports = await reportsQuery
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
                report.Findings,
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

    // =========================================================
    // CDE APPROVED / REJECTED LIST MAPPING
    // =========================================================

    private async Task<CdeInstitutionListDto> ToCdeInstitutionListDtoAsync(
        Institution institution,
        CancellationToken ct)
    {
        var assignments = await _db.InstitutionAssignments
            .Where(a =>
                a.InstitutionId == institution.Id &&
                a.Status != AssignmentStatus.Cancelled)
            .ToListAsync(ct);

        var scdeAssignment = assignments
            .Where(a => a.Role == AssignmentRole.SCDE)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

        var cqasoAssignment = assignments
            .Where(a => a.Role == AssignmentRole.CQASO)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

        string? scdeName = null;
        string? cqasoName = null;

        if (scdeAssignment != null)
        {
            scdeName = await _db.Users
                .Where(u => u.Id == scdeAssignment.AssignedToUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        if (cqasoAssignment != null)
        {
            cqasoName = await _db.Users
                .Where(u => u.Id == cqasoAssignment.AssignedToUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        return new CdeInstitutionListDto(
            institution.Id,
            institution.Name,
            institution.Uic,
            institution.County,
            institution.SubCounty,
            institution.ApprovalStartedAt,
            institution.Status.ToString(),
            institution.DecisionReason,
            institution.DecidedAt,
            scdeName,
            cqasoName);
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

        var canDecide = await CanDecideCoreAsync(institution.Id, ct);

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
            throw new WorkflowException(
                "You can only access institutions in your county.",
                403);
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
