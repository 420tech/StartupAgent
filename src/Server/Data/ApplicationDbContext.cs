using Microsoft.EntityFrameworkCore;
using StartupAgent.Shared.Models;
using StartupAgent.Models.Bookings;

namespace StartupAgent.Data;

/// <summary>
/// Application database context for StartupAgent.
/// Manages all entity configurations and migrations.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Founders table.
    /// </summary>
    public DbSet<Founder> Founders { get; set; }

    /// <summary>
    /// Sessions table (diagnostic progress tracking).
    /// </summary>
    public DbSet<Session> Sessions { get; set; }

    /// <summary>
    /// Assessments table (completed diagnostic results).
    /// </summary>
    public DbSet<Assessment> Assessments { get; set; }

    /// <summary>
    /// Deck analyses table (optional pitch deck AI analysis).
    /// </summary>
    public DbSet<DeckAnalysis> DeckAnalyses { get; set; }

    /// <summary>
    /// Booking events table (funnel tracking and conversion analysis).
    /// </summary>
    public DbSet<BookingEvent> BookingEvents { get; set; }

    /// <summary>
    /// Session drop-off events table (recovery email triggering).
    /// </summary>
    public DbSet<SessionDropOff> SessionDropOffs { get; set; }

    /// <summary>
    /// Recovery emails table (tracking sent recovery emails).
    /// </summary>
    public DbSet<RecoveryEmail> RecoveryEmails { get; set; }

    /// <summary>
    /// Deck analysis notifications table (success/failure alerts).
    /// </summary>
    public DbSet<DeckAnalysisNotification> DeckAnalysisNotifications { get; set; }

    /// <summary>
    /// Email templates table (transactional template system).
    /// </summary>
    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    /// <summary>
    /// Email template versions table (audit history).
    /// </summary>
    public DbSet<EmailTemplateVersion> EmailTemplateVersions { get; set; }

    /// <summary>
    /// Email template A/B tests table.
    /// </summary>
    public DbSet<EmailTemplateABTest> EmailTemplateABTests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Founder entity
        modelBuilder.Entity<Founder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.StartupName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate();

            // Unique email constraint
            entity.HasIndex(e => e.Email).IsUnique();

            // Relationships
            entity.HasMany(e => e.Sessions)
                .WithOne(s => s.Founder)
                .HasForeignKey(s => s.FounderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Assessments)
                .WithOne(a => a.Founder)
                .HasForeignKey(a => a.FounderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FounderId).IsRequired();
            entity.Property(e => e.ProgressState).IsRequired();
            entity.Property(e => e.AnswersJson).IsRequired().HasDefaultValue("{}");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate();
            
            // Optimistic concurrency control
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnName("RowVersion");

            // Indexes for fast queries
            entity.HasIndex(e => e.FounderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure Assessment entity
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FounderId).IsRequired();
            entity.Property(e => e.OverallScore).IsRequired();
            entity.Property(e => e.DimensionScoresJson).IsRequired().HasDefaultValue("{}");
            entity.Property(e => e.RoadmapText).IsRequired();
            entity.Property(e => e.RiskBriefText).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();

            // Indexes for fast queries
            entity.HasIndex(e => e.FounderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // Relationship to DeckAnalysis (one-to-one)
            entity.HasOne(a => a.DeckAnalysis)
                .WithOne(d => d.Assessment)
                .HasForeignKey<DeckAnalysis>(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DeckAnalysis entity
        modelBuilder.Entity<DeckAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssessmentId).IsRequired();
            entity.Property(e => e.FileUrl).IsRequired();
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(500);
            entity.Property(e => e.FileSizeBytes).IsRequired();
            entity.Property(e => e.InsightsJson).IsRequired().HasDefaultValue("{}");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt);

            // Indexes for fast queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure BookingEvent entity
        modelBuilder.Entity<BookingEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FounderId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(36);
            entity.Property(e => e.BookingId).HasMaxLength(255);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();

            // Indexes for fast queries
            entity.HasIndex(e => e.FounderId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.FounderId, e.CreatedAt }); // For funnel queries

            // Foreign key to Founder (without navigation property on Founder side)
            entity.HasOne<Founder>()
                .WithMany()
                .HasForeignKey(e => e.FounderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SessionDropOff entity
        modelBuilder.Entity<SessionDropOff>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FounderId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AssessmentId);
            entity.Property(e => e.LastActivityAt).IsRequired();
            entity.Property(e => e.Reason).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);

            // Indexes for fast queries
            entity.HasIndex(e => e.FounderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.FounderId, e.Status }); // For recovery job queries

            // Foreign key to Founder
            entity.HasOne<Founder>()
                .WithMany()
                .HasForeignKey(e => e.FounderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RecoveryEmail entity
        modelBuilder.Entity<RecoveryEmail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionDropOffId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.FounderId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ResumeLink).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.AttemptCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt);

            // Indexes for fast queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt }); // For job processor queries
            entity.HasIndex(e => e.FounderId);

            // Foreign key to Founder
            entity.HasOne<Founder>()
                .WithMany()
                .HasForeignKey(e => e.FounderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DeckAnalysisNotification entity
        modelBuilder.Entity<DeckAnalysisNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeckAnalysisId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.FounderId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.NotificationType).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.AttemptCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt);

            // Indexes for fast queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NotificationType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt }); // For job processor queries
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.FounderId);

            // Foreign key to Founder
            entity.HasOne<Founder>()
                .WithMany()
                .HasForeignKey(e => e.FounderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EmailTemplate entity
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Language).IsRequired();
            entity.Property(e => e.Subject).IsRequired();
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.PlainTextBody).IsRequired();
            entity.Property(e => e.Variables).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.ABTestVariant).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(255).HasDefaultValue("system");
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(255).HasDefaultValue("system");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);

            // Indexes for fast queries
            entity.HasIndex(e => new { e.TemplateCode, e.Language });
            entity.HasIndex(e => new { e.Type, e.IsActive });
            entity.HasIndex(e => e.ABTestId);

            // Relationship to EmailTemplateVersion
            entity.HasMany<EmailTemplateVersion>()
                .WithOne()
                .HasForeignKey(v => v.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EmailTemplateVersion entity
        modelBuilder.Entity<EmailTemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Subject).IsRequired();
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.PlainTextBody).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();

            // Indexes for fast queries
            entity.HasIndex(e => new { e.TemplateId, e.Version });
        });

        // Configure EmailTemplateABTest entity
        modelBuilder.Entity<EmailTemplateABTest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ControlVariant).IsRequired().HasMaxLength(50).HasDefaultValue("control");
            entity.Property(e => e.TestVariants).IsRequired();
            entity.Property(e => e.WinnerVariant).HasMaxLength(50);
            entity.Property(e => e.ControlSentCount).HasDefaultValue(0);
            entity.Property(e => e.VariantSentCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(255).HasDefaultValue("system");
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();

            // Indexes for fast queries
            entity.HasIndex(e => e.TemplateCode);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
