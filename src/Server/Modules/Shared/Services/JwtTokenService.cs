using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StartupAgent.Shared.Contracts;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// JWT token generation and validation service.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token (15 min expiry).
    /// </summary>
    string GenerateAccessToken(string founderId, string email);

    /// <summary>
    /// Generate refresh token (7 day expiry).
    /// </summary>
    string GenerateRefreshToken(string founderId, string email);

    /// <summary>
    /// Validate and extract claims from access token.
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Validate and extract claims from refresh token (returns ClaimsPrincipal).
    /// </summary>
    ClaimsPrincipal? ValidateRefreshToken(string token);

    /// <summary>
    /// Validate refresh token and return extracted FounderId and email.
    /// </summary>
    (bool IsValid, string? FounderId, string? Email) ValidateRefreshTokenWithClaims(string token);
}

/// <summary>
/// JWT token service implementation using System.IdentityModel.Tokens.Jwt.
/// Tokens are signed with single key from Key Vault (90d rotation).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly SymmetricSecurityKey _signingKey;
    private const string JwtIssuer = "StartupAgent";
    private const string JwtAudience = "StartupAgent-API";
    private const int AccessTokenExpireMinutes = 15;
    private const int RefreshTokenExpireDays = 7;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load signing key from config (expects base64-encoded key from Key Vault)
        var signingKeyBase64 = _configuration["Auth:JwtSigningKey"]
            ?? throw new InvalidOperationException("Auth:JwtSigningKey not configured");

        var signingKeyBytes = Convert.FromBase64String(signingKeyBase64);
        _signingKey = new SymmetricSecurityKey(signingKeyBytes);
    }

    public string GenerateAccessToken(string founderId, string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, founderId),
            new Claim(ClaimTypes.Email, email),
            new Claim("FounderId", founderId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("TokenType", "Access")
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(AccessTokenExpireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(string founderId, string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, founderId),
            new Claim(ClaimTypes.Email, email),
            new Claim("FounderId", founderId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("TokenType", "Refresh")
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: now,
            expires: now.AddDays(RefreshTokenExpireDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        return ValidateToken(token, "Access");
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        return ValidateToken(token, "Refresh");
    }

    public (bool IsValid, string? FounderId, string? Email) ValidateRefreshTokenWithClaims(string token)
    {
        var principal = ValidateRefreshToken(token);
        if (principal == null)
        {
            return (false, null, null);
        }

        var founderId = principal.FindFirst("FounderId")?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        return (!string.IsNullOrEmpty(founderId) && !string.IsNullOrEmpty(email), founderId, email);
    }

    private ClaimsPrincipal? ValidateToken(string token, string expectedType)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = JwtIssuer,
                ValidateAudience = true,
                ValidAudience = JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            // Verify token type
            var tokenType = principal.FindFirst("TokenType")?.Value;
            if (tokenType != expectedType)
            {
                _logger.LogWarning("Token type mismatch: expected {ExpectedType}, got {ActualType}", expectedType, tokenType);
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token validation failed: {Exception}", ex.Message);
            return null;
        }
    }
}
