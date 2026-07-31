using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Zoom;

public sealed class ZoomUserOAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<ZoomOptions> options,
    IConfiguration configuration) : IZoomUserOAuthService
{
    private readonly ZoomOptions _options = options.Value;

    public bool IsUserOAuthConfigured => _options.IsUserOAuthConfigured;
    public bool IsServerOAuthConfigured => _options.IsConfigured;
    public string FrontendRedirectUri => _options.FrontendRedirectUri;

    public string BuildAuthorizeUrl(Guid teacherUserId)
    {
        if (!_options.IsUserOAuthConfigured)
        {
            throw new InvalidOperationException("Zoom user OAuth is not configured. Set Zoom:UserOAuthClientId and Zoom:UserOAuthClientSecret.");
        }

        var state = CreateState(teacherUserId);
        return "https://zoom.us/oauth/authorize"
            + $"?response_type=code"
            + $"&client_id={Uri.EscapeDataString(_options.UserOAuthClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_options.UserOAuthRedirectUri)}"
            + $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<ZoomOAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await RequestTokensAsync(
            $"grant_type=authorization_code&code={Uri.EscapeDataString(code)}&redirect_uri={Uri.EscapeDataString(_options.UserOAuthRedirectUri)}",
            cancellationToken);
    }

    public async Task<ZoomOAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await RequestTokensAsync(
            $"grant_type=refresh_token&refresh_token={Uri.EscapeDataString(refreshToken)}",
            cancellationToken);
    }

    public bool TryParseState(string state, out Guid teacherUserId)
    {
        teacherUserId = Guid.Empty;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = raw.Split('|');
            if (parts.Length != 3) return false;
            if (!Guid.TryParse(parts[0], out teacherUserId)) return false;
            if (!long.TryParse(parts[1], out var expiryUnix)) return false;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix) return false;
            var expected = ComputeHmac($"{parts[0]}|{parts[1]}");
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[2]));
        }
        catch
        {
            return false;
        }
    }

    private async Task<ZoomOAuthTokens> RequestTokensAsync(string formBody, CancellationToken cancellationToken)
    {
        if (!_options.IsUserOAuthConfigured)
        {
            throw new InvalidOperationException("Zoom user OAuth is not configured.");
        }

        var client = httpClientFactory.CreateClient(nameof(ZoomUserOAuthService));
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.UserOAuthClientId}:{_options.UserOAuthClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Zoom user token exchange failed ({(int)response.StatusCode}): {payload}");
        }

        var token = System.Text.Json.JsonSerializer.Deserialize<ZoomUserTokenResponse>(payload)
            ?? throw new InvalidOperationException("Empty Zoom user token response.");

        string? email = null;
        try
        {
            var api = httpClientFactory.CreateClient(nameof(ZoomMeetingClient));
            using var meRequest = new HttpRequestMessage(HttpMethod.Get, "users/me");
            meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var meResponse = await api.SendAsync(meRequest, cancellationToken);
            if (meResponse.IsSuccessStatusCode)
            {
                var mePayload = await meResponse.Content.ReadAsStringAsync(cancellationToken);
                var me = System.Text.Json.JsonSerializer.Deserialize<ZoomMeResponse>(mePayload);
                email = me?.Email;
            }
        }
        catch
        {
            // Email is optional metadata.
        }

        return new ZoomOAuthTokens(
            token.AccessToken,
            token.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 60)),
            email);
    }

    private string CreateState(Guid teacherUserId)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var body = $"{teacherUserId:D}|{expiry}";
        var sig = ComputeHmac(body);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body}|{sig}"));
    }

    private string ComputeHmac(string value)
    {
        var key = _options.StateSigningKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = configuration["Jwt:Key"] ?? "CodeKids-Zoom-State";
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private sealed record ZoomUserTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record ZoomMeResponse(
        [property: JsonPropertyName("email")] string? Email);
}
