
namespace KemisOnboarding.Api.Models;

public class StoredFileRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public string Category { get; set; } = string.Empty;

    // User who uploaded the file
    public string? UploadedByUserId { get; set; }

    // Optional link to an institution
    public int? InstitutionId { get; set; }

    // Allows files to be associated with other KEMIS modules
    public string? RelatedEntityType { get; set; }

    public string? RelatedEntityId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
