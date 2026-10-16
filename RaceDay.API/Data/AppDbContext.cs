using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;


namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Role>(e =>
        {
            e.HasKey(x => x.RoleID);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
            e.HasData(new Role { RoleID = 1, RoleName = "Organiser" },
                      new Role { RoleID = 2, RoleName = "Participant" });
        });

        mb.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Event>(e =>
        {
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.HasOne(x => x.Organiser).WithMany().HasForeignKey(x => x.OrganiserID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Category>(e =>
        {
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Enrolment>(e =>
        {
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantID)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => x.CategoryID)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
        });

        mb.Entity<Result>(e =>
        {
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrolmentID)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
