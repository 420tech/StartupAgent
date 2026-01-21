namespace StartupAgent.Server.Services.LLM;

public class LLMOptions
{
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "openrouter";
    public string Model { get; set; } = "google/gemini-1.5-flash";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxTokens { get; set; } = 400;
    public string? ApiKey { get; set; }
}
