using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StartupAgent.Data;
using StartupAgent.Modules.Shared.Middleware;
using StartupAgent.Modules.Shared.Services;
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

// Add controllers and validation
builder.Services.AddControllers();

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
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
