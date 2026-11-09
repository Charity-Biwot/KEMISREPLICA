namespace KemisOnboarding.Api.Models;

public class InstitutionAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public Guid AssignedToUserId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public AssignmentRole Role { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class InspectionReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public Guid? AssignmentId { get; set; }
    public InstitutionAssignment? Assignment { get; set; }

    public Guid AuthorUserId { get; set; }
    public ReportType Type { get; set; }
    public string Summary { get; set; } = string.Empty;
    public ReportRecommendation Recommendation { get; set; }
    public string? FileKey { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class ReviewComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int InstitutionId { get; set; }
    public Guid? ReportId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RecipientUserId { get; set; }
    public int? InstitutionId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}