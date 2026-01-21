using StartupAgent.Shared.Models;

namespace StartupAgent.Shared.Services.Email;

public class RenderedEmail
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public int Version { get; set; }
    public string Format { get; set; } = "html"; // html or plaintext
}

public interface ITemplateRenderer
{
    Task<RenderedEmail> RenderAsync(
        string templateCode,
        EmailTemplateLanguage language,
        Dictionary<string, string> variables,
        bool useHtml = true,
        CancellationToken cancellationToken = default);
}