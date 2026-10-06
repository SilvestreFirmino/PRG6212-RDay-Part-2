using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrollment> EventEnrollments => Set<EventEnrollment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        modelBuilder.Entity<User>().HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");

        modelBuilder.Entity<ParticipantProfile>().HasIndex(p => p.UserId).IsUnique();
        modelBuilder.Entity<ParticipantProfile>().HasOne(p => p.User).WithOne(u => u.ParticipantProfile)
            .HasForeignKey<ParticipantProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RaceEvent>().HasKey(e => e.EventId);
        modelBuilder.Entity<RaceEvent>().ToTable("Events");
        modelBuilder.Entity<RaceEvent>().HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(7, 2);
        modelBuilder.Entity<Category>().Property(c => c.EntryFee).HasPrecision(10, 2);
        modelBuilder.Entity<Category>().HasIndex(c => new { c.EventId, c.Name }).IsUnique();
        modelBuilder.Entity<Category>().HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventEnrollment>().HasKey(e => e.EnrollmentId);
        modelBuilder.Entity<EventEnrollment>().Property(e => e.FeePaid).HasPrecision(10, 2);
        modelBuilder.Entity<EventEnrollment>().HasIndex(e => new { e.EventId, e.ParticipantId }).IsUnique();
        modelBuilder.Entity<EventEnrollment>().HasIndex(e => new { e.EventId, e.BibNumber }).IsUnique().HasFilter("[BibNumber] IS NOT NULL");
        modelBuilder.Entity<EventEnrollment>().HasOne(e => e.Event).WithMany(r => r.Enrollments).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventEnrollment>().HasOne(e => e.Category).WithMany(c => c.Enrollments).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventEnrollment>().HasOne(e => e.Participant).WithMany(u => u.Enrollments).HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RaceResult>().HasKey(r => r.ResultId);
        modelBuilder.Entity<RaceResult>().ToTable("Results");
        modelBuilder.Entity<RaceResult>().HasIndex(r => r.EnrollmentId).IsUnique();
        modelBuilder.Entity<RaceResult>().HasOne(r => r.Enrollment).WithOne(e => e.Result).HasForeignKey<RaceResult>(r => r.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RaceResult>().HasOne(r => r.RecordedByOrganiser).WithMany(u => u.RecordedResults).HasForeignKey(r => r.RecordedByOrganiserId).OnDelete(DeleteBehavior.Restrict);
    }
}
