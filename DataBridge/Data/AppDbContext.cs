using DataBridge.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<MirrorJob> MirrorJobs { get; set; }
        public DbSet<JobSchedule> JobSchedules { get; set; }
        public DbSet<JobHistory> JobHistories { get; set; }
        public DbSet<EmailConfig> EmailConfigs { get; set; }
        public DbSet<MirrorTableRegistry> MirrorTableRegistries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User ──────────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Username).IsRequired().HasMaxLength(100);
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
                e.Property(x => x.Email).HasMaxLength(200);
                e.HasIndex(x => x.Username).IsUnique();
            });

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Administrator",
                Email = "admin@databridge.local",
                Role = Models.Enums.UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1)
            });

            // ── Source ────────────────────────────────────────────
            modelBuilder.Entity<Source>(e =>
            {
                e.ToTable("Sources");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.ConnectionString).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);
            });

            // ── MirrorJob ─────────────────────────────────────────
            modelBuilder.Entity<MirrorJob>(e =>
            {
                e.ToTable("MirrorJobs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.DestinationTable).IsRequired().HasMaxLength(200);
                e.Property(x => x.WatermarkColumn).HasMaxLength(100);
                e.HasOne(x => x.Source)
                 .WithMany(x => x.MirrorJobs)
                 .HasForeignKey(x => x.SourceId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── JobSchedule ───────────────────────────────────────
            modelBuilder.Entity<JobSchedule>(e =>
            {
                e.ToTable("JobSchedules");
                e.HasKey(x => x.Id);
                e.Property(x => x.CronExpression).IsRequired().HasMaxLength(100);
                e.HasOne(x => x.MirrorJob)
                 .WithOne(x => x.Schedule)
                 .HasForeignKey<JobSchedule>(x => x.MirrorJobId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── JobHistory ────────────────────────────────────────
            modelBuilder.Entity<JobHistory>(e =>
            {
                e.ToTable("JobHistories");
                e.HasKey(x => x.Id);
                e.Property(x => x.TriggeredBy).HasMaxLength(100);
                e.HasOne(x => x.MirrorJob)
                 .WithMany(x => x.Histories)
                 .HasForeignKey(x => x.MirrorJobId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── EmailConfig ───────────────────────────────────────
            modelBuilder.Entity<EmailConfig>(e =>
            {
                e.ToTable("EmailConfigs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Recipients).IsRequired().HasMaxLength(1000);
                e.Property(x => x.SmtpHost).HasMaxLength(200);
                e.Property(x => x.SmtpUser).HasMaxLength(200);
                e.Property(x => x.SenderEmail).HasMaxLength(200);
                e.Property(x => x.SenderName).HasMaxLength(100);
                e.HasOne(x => x.MirrorJob)
                 .WithOne(x => x.EmailConfig)
                 .HasForeignKey<EmailConfig>(x => x.MirrorJobId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}