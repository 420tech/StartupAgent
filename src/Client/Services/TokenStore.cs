using Microsoft.JSInterop;

namespace StartupAgent.Client.Services;

/// <summary>
/// Stores and retrieves the bearer access token for API calls.
/// Uses browser localStorage so token survives reloads during development.
/// </summary>
public class TokenStore
{
    private const string StorageKey = "sa-access-token";
    private readonly IJSRuntime _jsRuntime;
    private string? _token;

    public TokenStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string? AccessToken => _token;
    public bool HasToken => !string.IsNullOrWhiteSpace(_token);

    public async Task InitializeAsync()
    {
        _token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
    }

    public async Task SetTokenAsync(string? token)
    {
        _token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();

        if (string.IsNullOrEmpty(_token))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, _token);
        }
    }
}
