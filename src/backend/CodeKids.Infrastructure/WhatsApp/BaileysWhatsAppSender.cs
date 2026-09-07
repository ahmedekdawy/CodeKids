using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Sends WhatsApp messages through a Node/Baileys gateway, picking a connected
/// session round-robin while honouring per-session daily limits and cooldowns.
/// </summary>
public sealed class BaileysWhatsAppSender(
    IHttpClientFactory httpClientFactory,
    IOptions<BaileysGatewayOptions> options,
    ILogger<BaileysWhatsAppSender> logger) : IWhatsAppMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BaileysGatewayOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, long> _sessionCooldowns = new();
    private int _lastUsedIndex = -1;

    public async Task<WhatsAppMessageResult> SendMessageAsync(
        string phone,
        string message,
        CancellationToken cancellationToken,
        string? ruleKey = null,
        string username = "system")
    {
        var jid = WhatsAppPhone.ToJid(phone);
        if (jid is null)
        {
            return WhatsAppMessageResult.Fail("رقم هاتف أو مجموعة غير صالح");
        }

        if (!_options.IsConfigured)
        {
            return WhatsAppMessageResult.Fail("WhatsApp gateway not configured.");
        }

        try
        {
            var client = CreateClient();
            var sessionId = await GetNextSessionAsync(client, cancellationToken);
            if (sessionId is null)
            {
                return WhatsAppMessageResult.Fail("لا توجد جلسات متاحة حالياً (مشغولة أو انتهى حد الإرسال اليومي)");
            }

            if (_options.SimulateTyping)
            {
                await SimulateTypingAsync(client, sessionId, jid, cancellationToken);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.SendTimeoutSeconds));

            using var response = await client.PostAsJsonAsync(
                $"sessions/{Uri.EscapeDataString(sessionId)}/send",
                new SendRequest(jid, message, ruleKey, username),
                timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return WhatsAppMessageResult.Fail($"WhatsApp send failed ({(int)response.StatusCode}): {body}");
            }

            _sessionCooldowns[sessionId] = Environment.TickCount64;
            return WhatsAppMessageResult.Ok(sessionId);
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

    public async Task<bool> IsNumberOnWhatsAppAsync(string phone, CancellationToken cancellationToken)
    {
        var number = WhatsAppPhone.ToDigits(phone);
        if (number is null || !_options.IsConfigured)
        {
            return false;
        }

        try
        {
            var client = CreateClient();
            var sessionId = await GetNextSessionAsync(client, cancellationToken);
            if (sessionId is null)
            {
                return false;
            }

            var result = await client.GetFromJsonAsync<OnWhatsAppResponse>(
                $"sessions/{Uri.EscapeDataString(sessionId)}/on-whatsapp?number={number}",
                JsonOptions,
                cancellationToken);

            return result?.Exists ?? false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WhatsApp number check failed");
            return false;
        }
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(nameof(BaileysWhatsAppSender));
        client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
        }

        return client;
    }

    private async Task<string?> GetNextSessionAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var sessions = await client.GetFromJsonAsync<List<SessionStatus>>("sessions", JsonOptions, cancellationToken)
            ?? [];

        var now = Environment.TickCount64;
        var available = sessions
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .Where(s => string.Equals(s.LiveStatus, "connected", StringComparison.OrdinalIgnoreCase))
            .Where(s => s.MaxMessages <= 0 || s.SentCount < s.MaxMessages)
            .Where(s => now - _sessionCooldowns.GetValueOrDefault(s.Id!) >= NextCooldown())
            .ToList();

        if (available.Count == 0)
        {
            return null;
        }

        var index = Interlocked.Increment(ref _lastUsedIndex);
        return available[(int)((uint)index % available.Count)].Id;
    }

    private int NextCooldown() =>
        Random.Shared.Next(_options.CooldownMinMilliseconds, Math.Max(_options.CooldownMinMilliseconds + 1, _options.CooldownMaxMilliseconds));

    private async Task SimulateTypingAsync(HttpClient client, string sessionId, string jid, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.PostAsJsonAsync(
                $"sessions/{Uri.EscapeDataString(sessionId)}/presence",
                new PresenceRequest(jid, "composing"),
                cancellationToken);

            var delay = Random.Shared.Next(_options.TypingMinMilliseconds, Math.Max(_options.TypingMinMilliseconds + 1, _options.TypingMaxMilliseconds));
            await Task.Delay(delay, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Presence update failed for {Jid}", jid);
        }
    }

    private sealed record SendRequest(
        [property: JsonPropertyName("jid")] string Jid,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("rule_key")] string? RuleKey,
        [property: JsonPropertyName("username")] string Username);

    private sealed record PresenceRequest(
        [property: JsonPropertyName("jid")] string Jid,
        [property: JsonPropertyName("presence")] string Presence);

    private sealed record SessionStatus
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("live_status")]
        public string? LiveStatus { get; init; }

        [JsonPropertyName("sent_count")]
        public int SentCount { get; init; }

        [JsonPropertyName("max_messages")]
        public int MaxMessages { get; init; }
    }

    private sealed record OnWhatsAppResponse
    {
        [JsonPropertyName("exists")]
        public bool Exists { get; init; }
    }
}
