using StartupAgent.Shared.Models;
using StartupAgent.Shared.Services.Email;

namespace StartupAgent.Server.Services.Email;

public class EmailTemplateRenderer : ITemplateRenderer
{
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(
        IEmailTemplateService templateService,
        ILogger<EmailTemplateRenderer> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<RenderedEmail> RenderAsync(
        string templateCode,
        EmailTemplateLanguage language,
        Dictionary<string, string> variables,
        bool useHtml = true,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateService.GetTemplateAsync(templateCode, language);
        if (template == null)
        {
            _logger.LogWarning("Template not found for rendering: {Code} ({Lang})", templateCode, language);
            return new RenderedEmail { Subject = string.Empty, Body = string.Empty, Format = useHtml ? "html" : "plaintext" };
        }

        var body = ReplacePlaceholders(useHtml ? template.HtmlBody : template.PlainTextBody, variables);
        var subject = ReplacePlaceholders(template.Subject, variables);

        return new RenderedEmail
        {
            Subject = subject,
            Body = body,
            TemplateId = template.Id,
            Version = template.Version,
            Format = useHtml ? "html" : "plaintext"
        };
    }

    private static string ReplacePlaceholders(string input, Dictionary<string, string> variables)
    {
        var output = input;
        foreach (var kvp in variables)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            output = output.Replace(placeholder, kvp.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        return output;
    }
}