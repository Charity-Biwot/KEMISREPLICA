using System.Text.Json.Serialization;

namespace KemisOnboarding.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssignmentRole
{
    SCDE = 0,
    CQASO = 1,
    QASO = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssignmentStatus { Pending = 0, ReportSubmitted = 1, Cancelled = 2 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportType { ScdeRecommendation = 0, QasoInspection = 1, CqasoInspection = 2 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportRecommendation { Approve = 0, Reject = 1, NeedsCorrection = 2 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    ApprovalRequested = 0,
    AssignmentMade = 1,
    ReportSubmitted = 2,
    InstitutionApproved = 3,
    InstitutionRejected = 4
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string CDE = "CDE";
    public const string SCDE = "SCDE";
    public const string QASO = "QASO";
    public const string CQASO = "CQASO";

    public static readonly string[] All = { Admin, CDE, SCDE, QASO, CQASO };
}