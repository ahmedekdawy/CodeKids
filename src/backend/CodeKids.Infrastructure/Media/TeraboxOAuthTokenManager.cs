using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Media;

public sealed class TeraboxOAuthTokenManager
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    private readonly TeraboxOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeraboxOAuthTokenManager> _logger;
    private readonly string _tokenFilePath;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private TeraboxOAuthTokenSnapshot _snapshot;

    public TeraboxOAuthTokenManager(
        IOptions<TeraboxOptions> options,
        IHttpClientFactory httpClientFactory,
        IHostEnvironment environment,
        ILogger<TeraboxOAuthTokenManager> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        var dataDir = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _tokenFilePath = Path.Combine(dataDir, "terabox-oauth.json");
        _snapshot = LoadOrDefault();
    }

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_options.ClientId)
        && !string.IsNullOrWhiteSpace(_options.ClientSecret)
        && !string.IsNullOrWhiteSpace(_options.PrivateSecret)
        && !string.IsNullOrWhiteSpace(GetRefreshToken());

    public async Task<TeraboxOAuthSession> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Terabox OAuth is not configured.");
        }

        if (NeedsRefresh())
        {
            await RefreshAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_snapshot.AccessToken))
        {
            throw new InvalidOperationException(
                "Terabox OAuth access token is missing. Complete Terabox authorization and set RefreshToken in config.");
        }

        return new TeraboxOAuthSession(
            _snapshot.AccessToken,
            NormalizeDomain(_snapshot.ApiDomain, _options.BaseUrl),
            NormalizeDomain(_snapshot.UploadDomain, "https://c-jp.1024terabox.com"));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!NeedsRefresh())
            {
                return;
            }

            var refreshToken = GetRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidOperationException("Terabox OAuth refresh token is not configured.");
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = BuildSign(timestamp);
            var oauthBase = GetOAuthBaseUrl();
            var body = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["timestamp"] = timestamp.ToString(),
                ["sign"] = sign
            };

            using var client = CreateOAuthClient();
            using var content = new FormUrlEncodedContent(body);
            using var response = await client.PostAsync($"{oauthBase}/oauth/refreshtoken", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Terabox OAuth refresh failed ({(int)response.StatusCode}): {json}");
            }

            var payload = JsonSerializer.Deserialize<TeraboxOAuthEnvelope<TeraboxOAuthTokenData>>(json);
            if (payload?.Errno is not 0 || payload.Data is null)
            {
                throw new InvalidOperationException(
                    $"Terabox OAuth refresh failed ({payload?.Errno}): {payload?.ShowMsg ?? json}");
            }

            _snapshot = _snapshot with
            {
                AccessToken = payload.Data.AccessToken,
                RefreshToken = payload.Data.RefreshToken,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(payload.Data.ExpiresIn - 60, 60))
            };

            await UpdateTokenInfoAsync(client, oauthBase, cancellationToken);
            SaveSnapshot();
            _logger.LogInformation("Terabox OAuth tokens refreshed; next refresh due at {ExpiresAtUtc:o}.", _snapshot.ExpiresAtUtc);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task UpdateTokenInfoAsync(HttpClient client, string oauthBase, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["access_token"] = _snapshot.AccessToken
        });
        using var response = await client.PostAsync($"{oauthBase}/oauth/tokeninfo", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Terabox OAuth tokeninfo failed ({Status}): {Body}", (int)response.StatusCode, json);
            return;
        }

        var payload = JsonSerializer.Deserialize<TeraboxOAuthEnvelope<TeraboxOAuthTokenInfoData>>(json);
        if (payload?.Errno is not 0 || payload.Data is null)
        {
            _logger.LogWarning("Terabox OAuth tokeninfo returned errno {Errno}: {Body}", payload?.Errno, json);
            return;
        }

        _snapshot = _snapshot with
        {
            ApiDomain = payload.Data.ApiDomain,
            UploadDomain = payload.Data.UploadDomain
        };
    }

    private bool NeedsRefresh() =>
        string.IsNullOrWhiteSpace(_snapshot.AccessToken)
        || _snapshot.ExpiresAtUtc <= DateTimeOffset.UtcNow.Add(RefreshSkew);

    private string GetRefreshToken() =>
        string.IsNullOrWhiteSpace(_snapshot.RefreshToken) ? _options.RefreshToken : _snapshot.RefreshToken;

    private TeraboxOAuthTokenSnapshot LoadOrDefault()
    {
        if (File.Exists(_tokenFilePath))
        {
            try
            {
                var json = File.ReadAllText(_tokenFilePath);
                var saved = JsonSerializer.Deserialize<TeraboxOAuthTokenSnapshot>(json);
                if (saved is not null && !string.IsNullOrWhiteSpace(saved.RefreshToken))
                {
                    return saved;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load Terabox OAuth token file; falling back to appsettings.");
            }
        }

        return new TeraboxOAuthTokenSnapshot
        {
            AccessToken = _options.AccessToken,
            RefreshToken = _options.RefreshToken,
            ExpiresAtUtc = default,
            ApiDomain = string.Empty,
            UploadDomain = string.Empty
        };
    }

    private void SaveSnapshot()
    {
        var json = JsonSerializer.Serialize(_snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_tokenFilePath, json);
    }

    private string BuildSign(long timestamp)
    {
        var payload = $"{_options.ClientId}_{timestamp}_{_options.ClientSecret}_{_options.PrivateSecret}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string GetOAuthBaseUrl()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        if (baseUrl.Contains("1024terabox", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.terabox.com";
        }

        return baseUrl;
    }

    private static string NormalizeDomain(string? domain, string fallback)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return fallback.TrimEnd('/');
        }

        return domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? domain.TrimEnd('/')
            : $"https://{domain.Trim('/')}";
    }

    private HttpClient CreateOAuthClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(TeraboxClient));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return client;
    }

    private sealed class TeraboxOAuthEnvelope<T>
    {
        [JsonPropertyName("errno")]
        public int Errno { get; set; }

        [JsonPropertyName("show_msg")]
        public string? ShowMsg { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class TeraboxOAuthTokenData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class TeraboxOAuthTokenInfoData
    {
        [JsonPropertyName("api_domain")]
        public string ApiDomain { get; set; } = string.Empty;

        [JsonPropertyName("upload_domain")]
        public string UploadDomain { get; set; } = string.Empty;
    }
}

public sealed record TeraboxOAuthSession(string AccessToken, string ApiDomain, string UploadDomain);

internal sealed record TeraboxOAuthTokenSnapshot
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public string ApiDomain { get; init; } = string.Empty;
    public string UploadDomain { get; init; } = string.Empty;
}
