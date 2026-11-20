
using Microsoft.EntityFrameworkCore;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<InstitutionAssignment> InstitutionAssignments => Set<InstitutionAssignment>();
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // QASO inspection records and supporting evidence
    public DbSet<QasoInspection> QasoInspections => Set<QasoInspection>();
    public DbSet<InspectionEvidence> InspectionEvidence => Set<InspectionEvidence>();
    public DbSet<StoredFileRecord> StoredFiles => Set<StoredFileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<StoredFileRecord>(e =>
{
    e.Property(f => f.OriginalFileName).HasMaxLength(255);
    e.Property(f => f.StoredFileName).HasMaxLength(255);
    e.Property(f => f.RelativePath).HasMaxLength(500);
    e.Property(f => f.ContentType).HasMaxLength(150);
    e.Property(f => f.Category).HasMaxLength(100);
    e.Property(f => f.UploadedByUserId).HasMaxLength(450);
    e.Property(f => f.RelatedEntityType).HasMaxLength(100);
    e.Property(f => f.RelatedEntityId).HasMaxLength(100);

    e.HasIndex(f => f.Category);
    e.HasIndex(f => f.InstitutionId);
    e.HasIndex(f => f.UploadedAt);
    e.HasIndex(f => new { f.RelatedEntityType, f.RelatedEntityId });
});

        modelBuilder.Entity<Institution>(e =>
        {
            e.HasIndex(i => i.Uic).IsUnique();
            e.HasIndex(i => new { i.NormalizedName, i.Level, i.SubCounty });
            e.HasIndex(i => i.NormalizedCertificateNumber);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<InstitutionAssignment>(e =>
        {
            e.HasOne(a => a.Institution)
                .WithMany()
                .HasForeignKey(a => a.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.InstitutionId, a.Role });
            e.HasIndex(a => a.AssignedToUserId);
        });

        modelBuilder.Entity<InspectionReport>(e =>
        {
            e.HasOne(r => r.Institution)
                .WithMany()
                .HasForeignKey(r => r.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Assignment)
                .WithMany()
                .HasForeignKey(r => r.AssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(r => new { r.InstitutionId, r.Type });
        });

        modelBuilder.Entity<ReviewComment>(e =>
        {
            e.HasIndex(c => c.InstitutionId);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        });

        // QASO inspection: one inspection record per QASO assignment
        modelBuilder.Entity<QasoInspection>(e =>
        {
            e.HasOne(q => q.Assignment)
                .WithOne()
                .HasForeignKey<QasoInspection>(q => q.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(q => q.AssignmentId)
                .IsUnique();
        });

        // Multiple evidence files can belong to one QASO inspection
        modelBuilder.Entity<InspectionEvidence>(e =>
        {
            e.HasOne(evidence => evidence.QasoInspection)
                .WithMany(inspection => inspection.EvidenceFiles)
                .HasForeignKey(evidence => evidence.QasoInspectionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(evidence => evidence.QasoInspectionId);
        });
    }
}
