using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Shared.Contracts;
using StartupAgent.Modules.Shared.Middleware;
using StartupAgent.Modules.Shared.Services;

namespace StartupAgent.Controllers;

/// <summary>
/// Authentication API endpoints for magic link and JWT-based authentication.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMagicLinkService _magicLinkService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IJwtTokenService jwtTokenService,
        IMagicLinkService magicLinkService,
        ILogger<AuthController> logger)
    {
        _jwtTokenService = jwtTokenService;
        _magicLinkService = magicLinkService;
        _logger = logger;
    }

    /// <summary>
    /// Send a magic link to the user's email address.
    /// </summary>
    /// <param name="request">The magic link request containing the user's email.</param>
    /// <returns>Confirmation that the magic link was sent.</returns>
    [HttpPost("magic-link")]
    [AllowAnonymous]
    public async Task<ActionResult<MagicLinkResponseDto>> SendMagicLink(
        [FromBody] MagicLinkRequestDto request)
    {
        var correlationId = HttpContext.GetCorrelationId();
        _logger.LogInformation(
            "Magic link requested for email: {Email} [CorrelationId: {CorrelationId}]",
            request.Email, correlationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate email format (basic validation)
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
        {
            return BadRequest(new { error = "Invalid email format" });
        }

        // Generate magic link token
        var token = _magicLinkService.GenerateToken(request.Email);

        // TODO: Send email via SendGrid with magic link
        // The token should be appended to the client URL like:
        // https://app.example.com/auth/verify?token={token}

        _logger.LogInformation(
            "Magic link generated for email: {Email} [CorrelationId: {CorrelationId}]",
            request.Email, correlationId);

        return Ok(new MagicLinkResponseDto
        {
            Message = "Magic link sent to your email address",
            Email = request.Email
        });
    }

    /// <summary>
    /// Verify a magic link token and return JWT tokens.
    /// </summary>
    /// <param name="request">The verification request containing the magic link token.</param>
    /// <returns>Access and refresh tokens if verification succeeds.</returns>
    [HttpPost("verify")]
    [AllowAnonymous]
    public ActionResult<TokenResponseDto> VerifyMagicLink(
        [FromBody] MagicLinkVerifyRequestDto request)
    {
        var correlationId = HttpContext.GetCorrelationId();
        _logger.LogInformation(
            "Magic link verification requested [CorrelationId: {CorrelationId}]",
            correlationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "Token is required" });
        }

        // Validate the magic link token
        var email = _magicLinkService.ValidateToken(request.Token);

        if (email == null)
        {
            _logger.LogWarning(
                "Magic link verification failed - invalid or expired token [CorrelationId: {CorrelationId}]",
                correlationId);
            return Unauthorized(new { error = "Invalid or expired magic link" });
        }

        // For magic links, generate a FounderId (in production, this would be fetched from DB)
        // For this implementation, we'll use email hash as FounderId
        var founderId = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(email))).Substring(0, 8);

        // Generate JWT tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(founderId, email);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(founderId, email);

        _logger.LogInformation(
            "Magic link verified successfully for email: {Email}, FounderId: {FounderId} [CorrelationId: {CorrelationId}]",
            email, founderId, correlationId);

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            FounderId = founderId,
            Email = email,
            ExpiresIn = 900 // 15 minutes in seconds
        });
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>New access and refresh tokens if successful.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public ActionResult<TokenResponseDto> RefreshToken(
        [FromBody] RefreshTokenRequestDto request)
    {
        var correlationId = HttpContext.GetCorrelationId();
        _logger.LogInformation(
            "Token refresh requested [CorrelationId: {CorrelationId}]",
            correlationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        // Validate the refresh token
        var (isValid, founderId, email) = _jwtTokenService.ValidateRefreshTokenWithClaims(request.RefreshToken);

        if (!isValid || string.IsNullOrEmpty(founderId) || string.IsNullOrEmpty(email))
        {
            _logger.LogWarning(
                "Token refresh failed - invalid or expired refresh token [CorrelationId: {CorrelationId}]",
                correlationId);
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }

        // Generate new JWT tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(founderId, email);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(founderId, email);

        _logger.LogInformation(
            "Token refreshed successfully for FounderId: {FounderId} [CorrelationId: {CorrelationId}]",
            founderId, correlationId);

        return Ok(new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            FounderId = founderId,
            Email = email,
            ExpiresIn = 900 // 15 minutes in seconds
        });
    }
}
