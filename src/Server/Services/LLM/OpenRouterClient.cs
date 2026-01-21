using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace StartupAgent.Server.Services.LLM;

public class OpenRouterClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly LLMOptions _options;
    private readonly ILogger<OpenRouterClient> _logger;
    private readonly string? _apiKey;

    public OpenRouterClient(
        HttpClient httpClient,
        IOptions<LLMOptions> options,
        IConfiguration configuration,
        ILogger<OpenRouterClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _apiKey = _options.ApiKey
            ?? configuration["OPENROUTER_API_KEY"]
            ?? configuration["OpenRouter:ApiKey"];
    }

    public async Task<string?> CompleteChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenRouter API key not configured; skipping LLM call");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("X-Title", "StartupAgent");

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "google/gemini-1.5-flash" : _options.Model,
            max_tokens = _options.MaxTokens,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenRouter call failed: {Status} {Body}", (int)response.StatusCode, body);
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                _logger.LogWarning("OpenRouter response missing choices");
                return null;
            }

            var content = choices[0].GetProperty("message").GetProperty("content").GetString();
            return content;
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is JsonException || ex is HttpRequestException)
        {
            _logger.LogWarning(ex, "OpenRouter call failed");
            return null;
        }
    }
}
