namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Magic link token generation and validation service.
/// </summary>
public interface IMagicLinkService
{
    /// <summary>
    /// Generate a magic link token for email authentication.
    /// </summary>
    string GenerateToken(string email);

    /// <summary>
    /// Validate magic link token and extract email.
    /// Returns email if valid, null if expired/invalid.
    /// </summary>
    string? ValidateToken(string token);
}

/// <summary>
/// Magic link service using HMAC-based token generation.
/// Tokens are single-use and expire after 15 minutes.
/// Format: {email}:{timestamp}:{hmac}
/// </summary>
public class MagicLinkService : IMagicLinkService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MagicLinkService> _logger;
    private const int TokenExpireMinutes = 15;

    public MagicLinkService(IConfiguration configuration, ILogger<MagicLinkService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(string email)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hmacSecret = _configuration["Auth:MagicLinkSecret"]
            ?? throw new InvalidOperationException("Auth:MagicLinkSecret not configured");

        var message = $"{email}:{timestamp}";
        var hmac = ComputeHmac(message, hmacSecret);

        var token = $"{message}:{hmac}";
        
        // Log token for development/testing (remove in production)
        _logger.LogWarning("🔑 MAGIC LINK TOKEN for {Email}: {Token}", email, token);
        
        return token;
    }

    public string? ValidateToken(string token)
    {
        try
        {
            var parts = token.Split(':');
            if (parts.Length != 3)
            {
                _logger.LogWarning("Magic link token format invalid");
                return null;
            }

            var email = parts[0];
            var timestampStr = parts[1];
            var providedHmac = parts[2];

            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                _logger.LogWarning("Magic link email invalid: {Email}", email);
                return null;
            }

            // Parse timestamp
            if (!long.TryParse(timestampStr, out var timestamp))
            {
                _logger.LogWarning("Magic link timestamp invalid: {Timestamp}", timestampStr);
                return null;
            }

            // Check expiry
            var tokenTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            if (DateTime.UtcNow.Subtract(tokenTime).TotalMinutes > TokenExpireMinutes)
            {
                _logger.LogInformation("Magic link token expired for {Email}", email);
                return null;
            }

            // Verify HMAC
            var hmacSecret = _configuration["Auth:MagicLinkSecret"]
                ?? throw new InvalidOperationException("Auth:MagicLinkSecret not configured");

            var message = $"{email}:{timestampStr}";
            var expectedHmac = ComputeHmac(message, hmacSecret);

            if (!string.Equals(providedHmac, expectedHmac, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Magic link HMAC mismatch for {Email}", email);
                return null;
            }

            return email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Magic link validation error");
            return null;
        }
    }

    private static string ComputeHmac(string message, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret)
        );

        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
