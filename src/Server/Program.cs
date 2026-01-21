using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StartupAgent.Data;
using StartupAgent.Data.Repositories;
using StartupAgent.Modules.Shared.Middleware;
using StartupAgent.Modules.Shared.Services;
using StartupAgent.Shared.Services.Scoring;
using StartupAgent.Shared.Services.Narrative;
using StartupAgent.Shared.Services.Pdf;
using StartupAgent.Server.Services.Email;
using StartupAgent.Shared.Services.Email;
using StartupAgent.Shared.Services.Booking;
using StartupAgent.Server.Services.Bookings;
using StartupAgent.Server.Services.Storage;
using StartupAgent.Server.Services.Jobs;
using StartupAgent.Server.Services.Analysis;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.MigrationsAssembly("StartupAgent")));

// Configure JWT authentication
var jwtSigningKey = builder.Configuration["Auth:JwtSigningKey"]
    ?? throw new InvalidOperationException("Auth:JwtSigningKey not configured");

var signingKeyBytes = Convert.FromBase64String(jwtSigningKey);
var signingKey = new SymmetricSecurityKey(signingKeyBytes);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = "StartupAgent",
            ValidateAudience = true,
            ValidAudience = "StartupAgent-API",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Suppress exception to allow custom error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.NoResult();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy: User must be authenticated
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Policy: User must be authenticated and have valid FounderId claim
    options.AddPolicy("ValidFounder", policy =>
        policy
            .RequireAuthenticatedUser()
            .RequireClaim("FounderId"));
});

// Add services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IMagicLinkService, MagicLinkService>();
builder.Services.AddScoped<IFounderRepository, FounderRepository>();
builder.Services.AddScoped<IFounderService, FounderService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<IBookingEventRepository, BookingEventRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<IMindsetDetectionService, MindsetDetectionService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<INarrativeService, NarrativeService>();
builder.Services.AddScoped<IRoadmapPdfService, RoadmapPdfService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ITemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IBookingEmailService, BookingEmailService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingEventTrackingService, BookingEventTrackingService>();
builder.Services.AddScoped<IDeckUploadService, DeckUploadService>();
builder.Services.AddScoped<IDeckAnalysisService, DeckAnalysisService>();
builder.Services.AddScoped<IRecoveryEmailService, RecoveryEmailService>();
builder.Services.AddScoped<IDeckAnalysisNotificationService, DeckAnalysisNotificationService>();
builder.Services.AddScoped<ISessionDropOffService, SessionDropOffService>();

// Deck analysis background job processing
builder.Services.AddSingleton<IDeckAnalysisJobQueue, DeckAnalysisJobQueue>();
builder.Services.AddHostedService<DeckAnalysisJobProcessor>();

// Recovery email background job processing
builder.Services.AddSingleton<IRecoveryEmailJobQueue, RecoveryEmailJobQueue>();
builder.Services.AddHostedService<RecoveryEmailJobProcessor>();

// Deck analysis notifications background job processing
builder.Services.AddSingleton<IDeckAnalysisNotificationQueue, DeckAnalysisNotificationQueue>();
builder.Services.AddHostedService<DeckAnalysisNotificationProcessor>();

// Session cleanup background job (24h retention policy for incomplete sessions)
builder.Services.AddHostedService<SessionCleanupJobService>();

// Session inactivity detection background job (5-minute intervals)
builder.Services.AddHostedService<SessionInactivityDetectionJobService>();

// Add controllers and validation
builder.Services.AddControllers();

// Add antiforgery (required for Blazor)
builder.Services.AddAntiforgery();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseProblemDetails();
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files with additional MIME types for Blazor WASM
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".dat"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
