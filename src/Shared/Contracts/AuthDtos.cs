namespace StartupAgent.Shared.Contracts;

/// <summary>
/// Request to initiate magic link authentication flow.
/// </summary>
public class MagicLinkRequestDto
{
    /// <summary>
    /// Email address for magic link delivery.
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Response containing magic link delivery confirmation.
/// </summary>
public class MagicLinkResponseDto
{
    /// <summary>
    /// Confirmation message for user feedback.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Email address where link was sent.
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Request to exchange magic link token for JWT.
/// </summary>
public class MagicLinkVerifyRequestDto
{
    /// <summary>
    /// Token from email link.
    /// </summary>
    public required string Token { get; set; }
}

/// <summary>
/// JWT token response with access and refresh tokens.
/// </summary>
public class TokenResponseDto
{
    /// <summary>
    /// Access token for API requests (15 min expiry).
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens (7 day expiry).
    /// </summary>
    public required string RefreshToken { get; set; }

    /// <summary>
    /// Access token expiry in seconds.
    /// </summary>
    public int ExpiresIn { get; set; } = 900;

    /// <summary>
    /// Unique identifier for the authenticated founder.
    /// </summary>
    public required string FounderId { get; set; }

    /// <summary>
    /// Email of authenticated founder.
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Request to refresh access token.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token from previous authentication.
    /// </summary>
    public required string RefreshToken { get; set; }
}

/// <summary>
/// Authentication claim set for JWT.
/// </summary>
public class AuthClaimsDto
{
    /// <summary>
    /// Unique founder identifier.
    /// </summary>
    public required string FounderId { get; set; }

    /// <summary>
    /// Founder email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Token issue timestamp (UTC).
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Token expiry timestamp (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Unique token identifier for revocation tracking.
    /// </summary>
    public required string JwtId { get; set; }
}
