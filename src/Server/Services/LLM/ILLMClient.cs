namespace StartupAgent.Server.Services.LLM;

public interface ILLMClient
{
    Task<string?> CompleteChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
