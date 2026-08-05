using System.Text.Json;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Zoom;

public sealed class ZoomOAuthSettingsStore : IZoomOAuthSettingsStore
{
    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ZoomOptions _defaults;
    private ZoomUserOAuthSettingsSnapshot _current;

    public ZoomOAuthSettingsStore(IOptions<ZoomOptions> options, IHostEnvironment environment)
    {
        _defaults = options.Value;
        var dataDir = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "zoom-user-oauth.json");
        _current = LoadOrDefault();
    }

    public bool IsUserOAuthConfigured =>
        !string.IsNullOrWhiteSpace(_current.ClientId)
        && !string.IsNullOrWhiteSpace(_current.ClientSecret)
        && !string.IsNullOrWhiteSpace(_current.RedirectUri);

    public ZoomUserOAuthSettingsSnapshot Get()
    {
        lock (_lock)
        {
            return _current;
        }
    }

    public void Save(string clientId, string? clientSecret, string? redirectUri, string? frontendRedirectUri)
    {
        lock (_lock)
        {
            var id = clientId.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Zoom OAuth Client ID is required.");
            }

            var secret = string.IsNullOrWhiteSpace(clientSecret)
                ? _current.ClientSecret
                : clientSecret.Trim();

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Zoom OAuth Client Secret is required.");
            }

            var redirect = string.IsNullOrWhiteSpace(redirectUri)
                ? (string.IsNullOrWhiteSpace(_current.RedirectUri) ? _defaults.UserOAuthRedirectUri : _current.RedirectUri)
                : redirectUri.Trim();

            var frontend = string.IsNullOrWhiteSpace(frontendRedirectUri)
                ? (string.IsNullOrWhiteSpace(_current.FrontendRedirectUri) ? _defaults.FrontendRedirectUri : _current.FrontendRedirectUri)
                : frontendRedirectUri.Trim();

            if (string.IsNullOrWhiteSpace(redirect))
            {
                throw new InvalidOperationException("Zoom OAuth redirect URI is required.");
            }

            _current = new ZoomUserOAuthSettingsSnapshot(id, secret, redirect, frontend);
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }

    private ZoomUserOAuthSettingsSnapshot LoadOrDefault()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var saved = JsonSerializer.Deserialize<ZoomUserOAuthSettingsSnapshot>(json);
                if (saved is not null && !string.IsNullOrWhiteSpace(saved.ClientId))
                {
                    return saved with
                    {
                        RedirectUri = string.IsNullOrWhiteSpace(saved.RedirectUri)
                            ? _defaults.UserOAuthRedirectUri
                            : saved.RedirectUri,
                        FrontendRedirectUri = string.IsNullOrWhiteSpace(saved.FrontendRedirectUri)
                            ? _defaults.FrontendRedirectUri
                            : saved.FrontendRedirectUri
                    };
                }
            }
            catch
            {
                // Fall back to appsettings.
            }
        }

        return new ZoomUserOAuthSettingsSnapshot(
            _defaults.UserOAuthClientId,
            _defaults.UserOAuthClientSecret,
            _defaults.UserOAuthRedirectUri,
            _defaults.FrontendRedirectUri);
    }
}
