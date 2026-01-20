using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Modules.Shared.Services;
using StartupAgent.Shared.Contracts;

namespace StartupAgent.Controllers;

/// <summary>
/// Founder profile management API endpoints.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class FounderController : ControllerBase
{
    private readonly IFounderService _founderService;
    private readonly ILogger<FounderController> _logger;

    public FounderController(IFounderService founderService, ILogger<FounderController> logger)
    {
        _founderService = founderService;
        _logger = logger;
    }

    /// <summary>
    /// Get current founder's profile.
    /// Requires authentication and uses FounderId claim from JWT.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "ValidFounder")]
    public async Task<ActionResult<FounderDto>> GetCurrentProfile()
    {
        var founderId = User.FindFirst("FounderId")?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            _logger.LogWarning("Request without valid FounderId claim");
            return Unauthorized();
        }

        var founder = await _founderService.GetFounderByIdAsync(founderId);
        if (founder == null)
        {
            _logger.LogWarning("Founder not found: {FounderId}", founderId);
            return NotFound();
        }

        return Ok(founder);
    }

    /// <summary>
    /// Get founder profile by ID.
    /// Requires authentication.
    /// </summary>
    [HttpGet("{founderId}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<ActionResult<FounderDto>> GetFounderById(string founderId)
    {
        if (string.IsNullOrEmpty(founderId))
        {
            return BadRequest(new { error = "Founder ID is required" });
        }

        var founder = await _founderService.GetFounderByIdAsync(founderId);
        if (founder == null)
        {
            return NotFound();
        }

        return Ok(founder);
    }

    /// <summary>
    /// Update current founder's profile.
    /// Requires ValidFounder authorization.
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = "ValidFounder")]
    public async Task<ActionResult<FounderDto>> UpdateCurrentProfile([FromBody] CreateUpdateFounderDto dto)
    {
        var founderId = User.FindFirst("FounderId")?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            _logger.LogWarning("Request without valid FounderId claim");
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _founderService.UpdateFounderAsync(founderId, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update founder profile: {Exception}", ex.Message);
            return NotFound();
        }
    }

    /// <summary>
    /// Create a new founder profile.
    /// Called after magic link verification during auth flow.
    /// Requires Authenticated authorization only (new founders won't have FounderId claim yet).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Authenticated")]
    public async Task<ActionResult<FounderDto>> CreateProfile([FromBody] CreateUpdateFounderDto dto)
    {
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Request without valid email claim");
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var exists = await _founderService.FounderExistsByEmailAsync(email);
            if (exists)
            {
                return BadRequest(new { error = "Founder profile already exists for this email" });
            }

            var founder = await _founderService.CreateFounderAsync(email, dto);
            return CreatedAtAction(nameof(GetFounderById), new { founderId = founder.Id }, founder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create founder profile: {Exception}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
