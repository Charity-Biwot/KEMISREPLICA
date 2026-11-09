using Microsoft.EntityFrameworkCore;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<InstitutionAssignment> InstitutionAssignments => Set<InstitutionAssignment>();
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}