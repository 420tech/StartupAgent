using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StartupAgent.Shared.Contracts;

namespace StartupAgent.Client.Services;

/// <summary>
/// Thin HTTP client for session-related API calls.
/// </summary>
public class SessionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenStore _tokenStore;

    public SessionApiClient(HttpClient httpClient, TokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }

    public async Task<SessionDto?> GetCurrentSessionAsync()
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Get, "api/v1/session/current");
        return await SendAndReadOrNull<SessionDto>(request);
    }

    public async Task<SessionDto?> GetSessionAsync(string sessionId)
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Get, $"api/v1/session/{sessionId}");
        return await SendAndReadOrNull<SessionDto>(request);
    }

    public async Task<SessionDto> StartSessionAsync(StartSessionDto dto)
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Post, "api/v1/session/start", dto);
        return await SendAndReadOrThrow<SessionDto>(request);
    }

    public async Task<QuestionDto?> GetNextQuestionAsync(string sessionId)
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Get, $"api/v1/session/{sessionId}/next-question");
        return await SendAndReadOrNull<QuestionDto>(request, treatBadRequestAsNull: true);
    }

    public async Task SubmitAnswerAsync(string sessionId, SubmitAnswerDto dto)
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Post, $"api/v1/session/{sessionId}/answer", dto);
        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await SafeReadMessageAsync(response);
            throw new InvalidOperationException(message ?? "Unable to submit answer (400)." );
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<SessionResultsDto?> GetResultsAsync(string sessionId)
    {
        var request = await CreateAuthorizedRequest(HttpMethod.Get, $"api/v1/session/{sessionId}/results");
        return await SendAndReadOrNull<SessionResultsDto>(request, treatBadRequestAsNull: true);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequest(HttpMethod method, string url, object? payload = null)
    {
        if (!_tokenStore.HasToken || string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            throw new InvalidOperationException("Access token is required. Paste it in the UI to proceed.");
        }

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);

        if (payload != null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return request;
    }

    private async Task<T?> SendAndReadOrNull<T>(HttpRequestMessage request, bool treatBadRequestAsNull = false)
    {
        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (treatBadRequestAsNull && response.StatusCode == HttpStatusCode.BadRequest)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T> SendAndReadOrThrow<T>(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>();
        if (payload == null)
        {
            throw new InvalidOperationException("Unexpected empty response from server.");
        }

        return payload;
    }

    private static async Task<string?> SafeReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}
