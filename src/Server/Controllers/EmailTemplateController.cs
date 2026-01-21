using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Shared.DTOs.Email;
using StartupAgent.Shared.Models;
using StartupAgent.Server.Services.Email;

namespace StartupAgent.Server.Controllers;

/// <summary>
/// API controller for managing transactional email templates.
/// Only accessible to admin users.
/// </summary>
[ApiController]
[Route("api/v1/admin/templates")]
[Authorize]
public class EmailTemplateController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<EmailTemplateController> _logger;

    public EmailTemplateController(
        IEmailTemplateService templateService,
        ILogger<EmailTemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get active template by code and language.
    /// </summary>
    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<EmailTemplateResponseDto>> GetActiveTemplate(
        string code,
        [FromQuery] EmailTemplateLanguage language = EmailTemplateLanguage.English)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(code, language);

            if (template == null)
            {
                _logger.LogWarning("Template not found: Code={Code}, Language={Language}", code, language);
                return NotFound(new { message = $"Template not found: {code}" });
            }

            return Ok(EmailTemplateResponseDto.FromEmailTemplate(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template: Code={Code}", code);
            return StatusCode(500, new { message = "Error retrieving template" });
        }
    }

    /// <summary>
    /// Create a new email template.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmailTemplateResponseDto>> CreateTemplate(
        [FromBody] CreateEmailTemplateDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TemplateCode) ||
                string.IsNullOrWhiteSpace(dto.Subject) ||
                string.IsNullOrWhiteSpace(dto.HtmlBody))
            {
                return BadRequest(new { message = "TemplateCode, Subject, and HtmlBody are required" });
            }

            var template = await _templateService.CreateTemplateAsync(
                templateCode: dto.TemplateCode,
                name: dto.Name,
                type: dto.Type,
                language: dto.Language,
                subject: dto.Subject,
                htmlBody: dto.HtmlBody,
                plainTextBody: dto.PlainTextBody,
                variables: dto.Variables,
                description: dto.Description,
                createdBy: User?.Identity?.Name ?? "system");

            _logger.LogInformation(
                "Created template: Code={Code}, Language={Language}, Type={Type}",
                dto.TemplateCode, dto.Language, dto.Type);

            return CreatedAtAction(nameof(GetActiveTemplate), new { code = dto.TemplateCode }, 
                EmailTemplateResponseDto.FromEmailTemplate(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template: Code={Code}", dto.TemplateCode);
            return StatusCode(500, new { message = "Error creating template" });
        }
    }

    /// <summary>
    /// Update an existing template.
    /// </summary>
    [HttpPut("{code}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmailTemplateResponseDto>> UpdateTemplate(
        string code,
        [FromQuery] EmailTemplateLanguage language,
        [FromBody] UpdateEmailTemplateDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Subject) ||
                string.IsNullOrWhiteSpace(dto.HtmlBody))
            {
                return BadRequest(new { message = "Subject and HtmlBody are required" });
            }

            var template = await _templateService.UpdateTemplateAsync(
                templateCode: code,
                language: language,
                subject: dto.Subject,
                htmlBody: dto.HtmlBody,
                plainTextBody: dto.PlainTextBody,
                changeNotes: dto.ChangeNotes,
                updatedBy: User?.Identity?.Name ?? "system");

            _logger.LogInformation(
                "Updated template: Code={Code}, Language={Language}, NewVersion={Version}",
                code, language, template.Version);

            return Ok(EmailTemplateResponseDto.FromEmailTemplate(template));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Template not found for update: Code={Code}", code);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template: Code={Code}", code);
            return StatusCode(500, new { message = "Error updating template" });
        }
    }

    /// <summary>
    /// Get version history for a template (audit trail).
    /// </summary>
    [HttpGet("{templateId}/history")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<EmailTemplateVersionResponseDto>>> GetVersionHistory(string templateId)
    {
        try
        {
            var versions = await _templateService.GetVersionHistoryAsync(templateId);

            if (!versions.Any())
            {
                return NotFound(new { message = "No versions found for template" });
            }

            var response = versions
                .Select(EmailTemplateVersionResponseDto.FromTemplateVersion)
                .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version history: TemplateId={TemplateId}", templateId);
            return StatusCode(500, new { message = "Error retrieving version history" });
        }
    }

    /// <summary>
    /// Start an A/B test for a template.
    /// </summary>
    [HttpPost("{code}/ab-test")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ABTestResponseDto>> StartABTest(
        string code,
        [FromBody] CreateABTestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TestName) ||
                string.IsNullOrWhiteSpace(dto.ControlVariant) ||
                !dto.TestVariants.Any())
            {
                return BadRequest(new { message = "TestName, ControlVariant, and TestVariants are required" });
            }

            var test = await _templateService.StartABTestAsync(
                templateCode: code,
                testName: dto.TestName,
                controlVariant: dto.ControlVariant,
                testVariants: dto.TestVariants,
                description: dto.Description);

            _logger.LogInformation(
                "Started A/B test: Code={Code}, TestName={TestName}, Variants={Variants}",
                code, dto.TestName, string.Join(",", dto.TestVariants));

            return CreatedAtAction(nameof(GetActiveABTest), new { code }, 
                ABTestResponseDto.FromABTest(test));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start A/B test: Code={Code}", code);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting A/B test: Code={Code}", code);
            return StatusCode(500, new { message = "Error starting A/B test" });
        }
    }

    /// <summary>
    /// Get active A/B test for a template.
    /// </summary>
    [HttpGet("{code}/ab-test")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ABTestResponseDto>> GetActiveABTest(string code)
    {
        try
        {
            var test = await _templateService.GetActiveABTestAsync(code);

            if (test == null)
            {
                return NotFound(new { message = $"No active A/B test for template: {code}" });
            }

            return Ok(ABTestResponseDto.FromABTest(test));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving A/B test: Code={Code}", code);
            return StatusCode(500, new { message = "Error retrieving A/B test" });
        }
    }

    /// <summary>
    /// Conclude an A/B test and select the winner.
    /// </summary>
    [HttpPost("{testId}/ab-test/conclude")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ABTestResponseDto>> ConcludeABTest(
        string testId,
        [FromBody] ConcludeABTestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.WinnerVariant))
            {
                return BadRequest(new { message = "WinnerVariant is required" });
            }

            var test = await _templateService.ConcludeABTestAsync(testId, dto.WinnerVariant);

            _logger.LogInformation(
                "Concluded A/B test: TestId={TestId}, Winner={Winner}",
                testId, dto.WinnerVariant);

            return Ok(ABTestResponseDto.FromABTest(test));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error concluding A/B test: TestId={TestId}", testId);
            return StatusCode(500, new { message = "Error concluding A/B test" });
        }
    }

    /// <summary>
    /// Render a template with variables for preview/testing.
    /// </summary>
    [HttpPost("render")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RenderEmailTemplateResponseDto>> RenderTemplate(
        [FromBody] RenderEmailTemplateDto dto)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(dto.TemplateCode, dto.Language);

            if (template == null)
            {
                return NotFound(new { message = $"Template not found: {dto.TemplateCode}" });
            }

            var renderedBody = _templateService.RenderTemplate(template, dto.Variables, dto.UseHtml);
            var renderedSubject = _templateService.RenderTemplate(
                new EmailTemplate { Subject = template.Subject, HtmlBody = "", PlainTextBody = "" },
                dto.Variables, false).Split('\n')[0]; // Just use the subject rendering logic

            return Ok(new RenderEmailTemplateResponseDto
            {
                Subject = template.Subject,
                RenderedSubject = renderedSubject,
                Body = dto.UseHtml ? template.HtmlBody : template.PlainTextBody,
                RenderedBody = renderedBody,
                Format = dto.UseHtml ? "html" : "plaintext"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template: Code={Code}", dto.TemplateCode);
            return StatusCode(500, new { message = "Error rendering template" });
        }
    }
}

/// <summary>
/// DTO for concluding A/B test.
/// </summary>
public class ConcludeABTestDto
{
    public string WinnerVariant { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for rendered template preview.
/// </summary>
public class RenderEmailTemplateResponseDto
{
    public string Subject { get; set; } = string.Empty;
    public string RenderedSubject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string RenderedBody { get; set; } = string.Empty;
    public string Format { get; set; } = "html";
}
