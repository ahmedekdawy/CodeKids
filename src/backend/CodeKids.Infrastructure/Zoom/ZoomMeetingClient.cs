using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Zoom;

public sealed class ZoomMeetingClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ZoomOptions> options) : IZoomMeetingClient
{
    private readonly ZoomOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<ZoomMeetingResult> CreateMeetingAsync(ZoomMeetingRequest request, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(ZoomMeetingClient));
        string bearerToken;
        string meetingPath;

        if (!string.IsNullOrWhiteSpace(request.UserAccessToken))
        {
            bearerToken = request.UserAccessToken;
            meetingPath = "users/me/meetings";
        }
        else if (_options.IsConfigured)
        {
            bearerToken = await GetServerAccessTokenAsync(httpClient, cancellationToken);
            meetingPath = $"users/{Uri.EscapeDataString(_options.HostUserId)}/meetings";
        }
        else
        {
            var mockId = Random.Shared.NextInt64(100_000_000, 999_999_999).ToString();
            return new ZoomMeetingResult(
                mockId,
                $"https://zoom.us/j/{mockId}?pwd=codekids-dev",
                $"https://zoom.us/s/{mockId}?zak=codekids-dev-host");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, meetingPath);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        message.Content = JsonContent.Create(new ZoomCreateMeetingBody(
            request.Topic,
            2,
            request.StartsAtUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            request.DurationMinutes,
            "UTC",
            request.Agenda,
            new ZoomMeetingSettings(true, false, true)));

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Zoom meeting creation failed ({(int)response.StatusCode}): {payload}");
        }

        var created = System.Text.Json.JsonSerializer.Deserialize<ZoomCreateMeetingResponse>(payload)
            ?? throw new InvalidOperationException("Zoom returned an empty meeting response.");

        if (string.IsNullOrWhiteSpace(created.JoinUrl) || string.IsNullOrWhiteSpace(created.StartUrl))
        {
            throw new InvalidOperationException("Zoom meeting response was missing join or start URL.");
        }

        return new ZoomMeetingResult(
            created.Id.ToString(),
            created.JoinUrl,
            created.StartUrl);
    }

    private async Task<string> GetServerAccessTokenAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={Uri.EscapeDataString(_options.AccountId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Zoom OAuth token request failed ({(int)response.StatusCode}): {payload}");
            }

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<ZoomTokenResponse>(payload)
                ?? throw new InvalidOperationException("Zoom returned an empty OAuth token response.");

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, tokenResponse.ExpiresIn - 60));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed record ZoomCreateMeetingBody(
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("start_time")] string StartTime,
        [property: JsonPropertyName("duration")] int Duration,
        [property: JsonPropertyName("timezone")] string Timezone,
        [property: JsonPropertyName("agenda")] string Agenda,
        [property: JsonPropertyName("settings")] ZoomMeetingSettings Settings);

    private sealed record ZoomMeetingSettings(
        [property: JsonPropertyName("join_before_host")] bool JoinBeforeHost,
        [property: JsonPropertyName("waiting_room")] bool WaitingRoom,
        [property: JsonPropertyName("mute_upon_entry")] bool MuteUponEntry);

    private sealed record ZoomCreateMeetingResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("join_url")] string JoinUrl,
        [property: JsonPropertyName("start_url")] string StartUrl);

    private sealed record ZoomTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
