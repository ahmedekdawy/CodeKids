using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Sends WhatsApp messages through the whats-pro gateway. Request bodies are
/// {"payload": "&lt;CryptoJS AES ciphertext&gt;"}; the token returned by the login route
/// authenticates every call after it as a bearer token.
/// </summary>
public sealed class WhatsProSender(
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsProOptions> options,
    ILogger<WhatsProSender> logger) : IWhatsAppMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int LoginRetryDelayMs = 30_000;

    private readonly WhatsProOptions _options = options.Value;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private string? _token;
    private string? _loginError;
    private long _loginFailedAt;

    public async Task<WhatsAppMessageResult> SendMessageAsync(
        string phone,
        string message,
        CancellationToken cancellationToken,
        string? ruleKey = null,
        string username = "system")
    {
        if (!WhatsAppPhone.TryParseTarget(phone, out var target))
        {
            return WhatsAppMessageResult.Fail("رقم هاتف أو مجموعة غير صالح");
        }

        if (!_options.IsConfigured)
        {
            return WhatsAppMessageResult.Fail("WhatsApp gateway not configured.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.SendTimeoutSeconds));

            //var payload = new Dictionary<string, string>
            //{
            //    [_options.PhoneField] = destination,
            //    [_options.MessageField] = message
            //};
            var payload = target.IsGroup
                ? (object)new
                {
                    send_phone = false,
                    phones = Array.Empty<string>(),
                    send_group = true,
                    group_id = target.GroupId,
                    send_client = false,
                    client_ids = Array.Empty<int>(),
                    img = (string?)null,
                    client_default_phone = true,
                    send_all_clients = false,
                    message
                }
                : new
                {
                    send_phone = true,
                    phones = new[] { "+" + target.PhoneDigits },
                    send_group = false,
                    group_id = 0,
                    send_client = false,
                    client_ids = Array.Empty<int>(),
                    img = (string?)null,
                    client_default_phone = true,
                    send_all_clients = false,
                    message
                };

            using var response = await SendWithRetryAsync(_options.SendRoute, payload, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return WhatsAppMessageResult.Fail($"WhatsApp send failed ({(int)response.StatusCode}): {body}");
            }

            return WhatsAppMessageResult.Ok("whats-pro");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return WhatsAppMessageResult.Fail($"Send request timed out ({_options.SendTimeoutSeconds}s)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp send failed for rule {RuleKey}", ruleKey);
            return WhatsAppMessageResult.Fail(ex.Message);
        }
    }

    public Task<WhatsAppMessageResult> SendNotificationAsync(
        string phone,
        string template,
        IReadOnlyDictionary<string, string?> variables,
        CancellationToken cancellationToken,
        string? ruleKey = null,
        string username = "system")
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return Task.FromResult(WhatsAppMessageResult.Fail("Template not found"));
        }

        var message = WhatsAppTemplate.Render(template, variables);
        return SendMessageAsync(phone, message, cancellationToken, ruleKey, username);
    }

    /// <summary>The gateway exposes no number lookup, so a well-formed number is treated as reachable.</summary>
    public Task<bool> IsNumberOnWhatsAppAsync(string phone, CancellationToken cancellationToken) =>
        Task.FromResult(_options.IsConfigured && WhatsAppPhone.TryParseTarget(phone, out _));

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string route,
        object payload,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(false, cancellationToken);
        var response = await PostAsync(route, payload, token, cancellationToken);
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
        {
            return response;
        }

        // The token expired between calls, so log in again and replay once.
        response.Dispose();
        return await PostAsync(route, payload, await GetTokenAsync(true, cancellationToken), cancellationToken);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string route,
        object payload,
        string token,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var payloadenc = Encrypt(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, route.TrimStart('/'))
        {
            Content = JsonContent.Create(new PayloadRequest(payloadenc))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response= await client.SendAsync(request, cancellationToken);
        return response;
    }

    private async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _token is { Length: > 0 })
        {
            return _token;
        }

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _token is { Length: > 0 })
            {
                return _token;
            }

            // A caller sending to many numbers loops over SendMessageAsync, so without this
            // a rejected login would be retried once per recipient.
            if (_loginError is not null && Environment.TickCount64 - _loginFailedAt < LoginRetryDelayMs)
            {
                throw new InvalidOperationException(_loginError);
            }

            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(
                _options.LoginRoute.TrimStart('/'),
                new PayloadRequest(Encrypt(new Dictionary<string, string>
                {
                    ["username"] = _options.Username,
                    ["password"] = _options.Password,
                    ["email"] = _options.Username
                })),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"WhatsApp login failed ({(int)response.StatusCode}): {body}");
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);
            if (string.IsNullOrWhiteSpace(login?.Token))
            {
                throw new InvalidOperationException("WhatsApp login did not return a token.");
            }

            _token = login.Token;
            _loginError = null;
            return _token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _loginError = ex.Message;
            _loginFailedAt = Environment.TickCount64;
            throw;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private string Encrypt(object payload) =>
        CryptoJsAes.Encrypt(JsonSerializer.Serialize(payload), _options.EncryptionKey);

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(nameof(WhatsProSender));
        client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        return client;
    }

    private sealed record PayloadRequest(
        [property: JsonPropertyName("payload")] string Payload);

    private sealed record LoginResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; init; }

        [JsonPropertyName("access_token")]
        public string? Token { get; init; }
    }
}
