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

            // Seed default email templates
            var sessionRecoveryTemplate = new EmailTemplate
            {
                Id = "tpl-001",
                TemplateCode = "session-recovery-email",
                Name = "Session Recovery Email",
                Type = EmailTemplateType.SessionRecovery,
                Language = EmailTemplateLanguage.English,
                Subject = "{{founderName}}, Your StartupAgent Assessment is Waiting",
                HtmlBody = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }
            .header h1 { margin: 0; font-size: 24px; }
            .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }
            .button { display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }
            .highlight { background: #fffbcd; padding: 15px; border-left: 4px solid #ffc107; border-radius: 4px; margin: 20px 0; }
            .footer { text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""header"">
                <h1>Welcome Back, {{founderName}}!</h1>
            </div>
            <div class=""content"">
                <p>Hi {{founderName}},</p>
                <p>We noticed you stepped away from your StartupAgent diagnostic assessment. No worries! Your progress has been saved.</p>
                <div class=""highlight"">
                    <strong>Ready to continue?</strong> Click below to resume your assessment and unlock personalized insights for your startup.
                </div>
                <a href=""{{resumeLink}}"" class=""button"">Resume My Assessment</a>
                <p>Questions? We're here to help. Just reply to this email.</p>
            </div>
            <div class=""footer"">
                <p>© 2026 StartupAgent. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>",
                PlainTextBody = @"Hi {{founderName}},

    We noticed you stepped away from your StartupAgent diagnostic assessment. No worries! Your progress has been saved.

    Ready to continue? Click the link below to resume your assessment:
    {{resumeLink}}

    Questions? We're here to help. Just reply to this email.

    © 2026 StartupAgent. All rights reserved.",
                Variables = "founderName,resumeLink",
                Description = "Transactional email sent to founders who have dropped off from a session to encourage them to resume",
                IsActive = true,
                Version = 1,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                IsArchived = false
            };

            modelBuilder.Entity<EmailTemplate>().HasData(sessionRecoveryTemplate);

            // Seed booking confirmation template
            var bookingConfirmationTemplate = new EmailTemplate
            {
                Id = "tpl-002",
                TemplateCode = "booking-confirmation-email",
                Name = "Booking Confirmation Email",
                Type = EmailTemplateType.BookingConfirmation,
                Language = EmailTemplateLanguage.English,
                Subject = "Your Strategy Call is Confirmed, {{founderName}}",
                HtmlBody = @"<!DOCTYPE html>
    <html>
    <head>
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #0EA5E9, #06B6D4); color: white; padding: 24px; border-radius: 8px; margin-bottom: 24px; }
            .header h1 { margin: 0; font-size: 24px; }
            .content { background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 24px; }
            .detail { margin: 12px 0; }
            .detail-label { color: #666; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; }
            .detail-value { font-size: 16px; font-weight: 600; color: #0EA5E9; }
            .cta-button { display: inline-block; background: linear-gradient(135deg, #0EA5E9, #06B6D4); color: white; padding: 14px 32px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 20px 0; }
            .footer { color: #666; font-size: 12px; text-align: center; margin-top: 32px; }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""header"">
                <h1>Strategy Call Confirmed! 📅</h1>
            </div>

            <div class=""content"">
                <p>Hi {{founderName}},</p>

                <p>Your {{durationMinutes}}-minute strategy call with Tim is confirmed. Here are the details:</p>

                <div class=""detail"">
                    <div class=""detail-label"">Scheduled Date & Time</div>
                    <div class=""detail-value"">{{scheduledAt}}</div>
                </div>

                <div class=""detail"">
                    <div class=""detail-label"">Duration</div>
                    <div class=""detail-value"">{{durationMinutes}} minutes</div>
                </div>

                <div class=""detail"">
                    <div class=""detail-label"">Price</div>
                    <div class=""detail-value"">${{priceUsd}}</div>
                </div>

                <p>To complete payment and secure your spot, click the button below:</p>

                <a href=""{{paymentLink}}"" class=""cta-button"">Complete Payment</a>

                <p>A Zoom link will be sent to your email once payment is confirmed.</p>

                <p>Questions? Reply to this email and we'll help you out.</p>

                <p>Best,<br>Tim from StartupAgent</p>
            </div>

            <div class=""footer"">
                <p>Booking ID: {{bookingId}}<br>
                Session ID: {{sessionId}}</p>
            </div>
        </div>
    </body>
    </html>",
                PlainTextBody = @"Hi {{founderName}},

    Your {{durationMinutes}}-minute strategy call with Tim is confirmed.
    Scheduled: {{scheduledAt}}
    Price: ${{priceUsd}}

    Complete payment here: {{paymentLink}}

    Booking ID: {{bookingId}}
    Session ID: {{sessionId}}

    Best,
    StartupAgent",
                Variables = "founderName,scheduledAt,durationMinutes,priceUsd,paymentLink,bookingId,sessionId",
                Description = "Transactional confirmation email for booked strategy calls",
                IsActive = true,
                Version = 1,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                IsArchived = false
            };

            // Seed deck analysis success template
            var deckAnalysisSuccessTemplate = new EmailTemplate
            {
                Id = "tpl-003",
                TemplateCode = "deck-analysis-results-email",
                Name = "Deck Analysis Results Email",
                Type = EmailTemplateType.DeckAnalysisResults,
                Language = EmailTemplateLanguage.English,
                Subject = "Your Deck Analysis is Ready, {{founderName}}",
                HtmlBody = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }
            .header h1 { margin: 0; font-size: 24px; }
            .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }
            .success-badge { display: inline-block; background: #28a745; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; margin: 10px 0; }
            .button { display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }
            .highlight { background: #e8f5e9; padding: 15px; border-left: 4px solid #4caf50; border-radius: 4px; margin: 20px 0; }
            .footer { text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""header"">
                <h1>Your Deck Analysis is Ready! 🎉</h1>
            </div>
            <div class=""content"">
                <p>Hi {{founderName}},</p>

                <p>Great news! Your pitch deck has been analyzed and insights are ready.</p>

                <div class=""success-badge"">✓ Analysis Complete</div>

                <div class=""highlight"">
                    <strong>Analysis completed at {{completedTime}}</strong><br>
                    File: {{originalFileName}}
                </div>

                <p><strong>What's Next?</strong></p>
                <ul>
                    <li>Log in to your StartupAgent dashboard to view full insights</li>
                    <li>See TAM/GTM clarity, governance signals, and storytelling scores</li>
                    <li>Identify red flags and recommended follow-ups</li>
                </ul>

                <a href=""https://startupaigent.com/dashboard"" class=""button"">View Your Insights</a>

                <p style=""margin-top: 30px;"">
                    <strong>Want personalized guidance?</strong><br>
                    Book a call with Tim to discuss your deck analysis and get strategic recommendations.
                </p>
                <a href=""https://calendly.com/tim-startup"" class=""button"" style=""background: #764ba2;"">Book a Strategy Call</a>

                <p style=""margin-top: 30px; font-style: italic; color: #666;"">
                    Questions? We're here to help. Just reply to this email.
                </p>
            </div>

            <div class=""footer"">
                <p>© 2026 StartupAgent. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>",
                PlainTextBody = @"Hi {{founderName}},

    Your pitch deck has been analyzed and insights are ready.
    Completed at: {{completedTime}}
    File: {{originalFileName}}

    View your insights: https://startupaigent.com/dashboard

    © 2026 StartupAgent.",
                Variables = "founderName,completedTime,originalFileName",
                Description = "Notification email sent when deck analysis results are available",
                IsActive = true,
                Version = 1,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                IsArchived = false
            };

            // Seed deck analysis failure template
            var deckAnalysisFailureTemplate = new EmailTemplate
            {
                Id = "tpl-004",
                TemplateCode = "deck-analysis-failure-email",
                Name = "Deck Analysis Failure Email",
                Type = EmailTemplateType.Notification,
                Language = EmailTemplateLanguage.English,
                Subject = "Deck Analysis Issue: Action Needed",
                HtmlBody = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }
            .header h1 { margin: 0; font-size: 24px; }
            .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }
            .warning-badge { display: inline-block; background: #ff9800; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; margin: 10px 0; }
            .button { display: inline-block; background: #ff6b6b; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }
            .secondary-button { background: #667eea; }
            .highlight { background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; border-radius: 4px; margin: 20px 0; }
            .footer { text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""header"">
                <h1>Deck Analysis Update</h1>
            </div>
            <div class=""content"">
                <p>Hi {{founderName}},</p>

                <p>We encountered an issue while analyzing your pitch deck. This sometimes happens with complex files or specific formats.</p>

                <div class=""warning-badge"">⚠ Analysis Incomplete</div>

                <div class=""highlight"">
                    <strong>File:</strong> {{originalFileName}}<br>
                    <strong>Status:</strong> {{status}}
                </div>

                <p><strong>Here's what you can do:</strong></p>
                <ul>
                    <li><strong>Try again:</strong> Re-upload your deck file (PDF or PowerPoint)</li>
                    <li><strong>Manual review:</strong> Book a call with Tim for personalized feedback on your deck</li>
                    <li><strong>Contact support:</strong> Email us for help troubleshooting</li>
                </ul>

                <a href=""https://startupaigent.com/dashboard"" class=""button"">Try Again</a>

                <p style=""margin-top: 30px;"">
                    <strong>Prefer personalized guidance?</strong><br>
                    Book a call with Tim to discuss your deck directly. He can provide detailed feedback tailored to your startup's unique situation.
                </p>
                <a href=""https://calendly.com/tim-startup"" class=""button secondary-button"">Book a Strategy Call</a>

                <p style=""margin-top: 30px; font-style: italic; color: #666;"">
                    Questions? We're here to help. Just reply to this email or contact support.
                </p>
            </div>

            <div class=""footer"">
                <p>© 2026 StartupAgent. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>",
                PlainTextBody = @"Hi {{founderName}},

    We encountered an issue while analyzing your pitch deck.
    File: {{originalFileName}}
    Status: {{status}}

    Try again: https://startupaigent.com/dashboard
    Book a call: https://calendly.com/tim-startup

    © 2026 StartupAgent.",
                Variables = "founderName,originalFileName,status",
                Description = "Notification email sent when deck analysis fails",
                IsActive = true,
                Version = 1,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                IsArchived = false
            };

            modelBuilder.Entity<EmailTemplate>().HasData(
                bookingConfirmationTemplate,
                deckAnalysisSuccessTemplate,
                deckAnalysisFailureTemplate);
        }
    }
