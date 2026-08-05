using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.WhatsApp;

public sealed class WhatsAppClient(
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppOptions> options) : IWhatsAppClient
{
    private readonly WhatsAppOptions _options = options.Value;

    public string BuildShareUrl(string message) =>
        $"https://wa.me/?text={Uri.EscapeDataString(message)}";

    public async Task<WhatsAppSendResult> SendTextAsync(string phoneE164, string message, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(phoneE164);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return new WhatsAppSendResult(false, "Invalid phone number.");
        }

        if (!_options.IsConfigured)
        {
            return new WhatsAppSendResult(
                false,
                $"WhatsApp API not configured. Share link: {BuildShareUrl(message)}");
        }

        var client = httpClientFactory.CreateClient(nameof(WhatsAppClient));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.ApiVersion}/{_options.PhoneNumberId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = JsonContent.Create(new WhatsAppTextPayload(
            "whatsapp",
            phone,
            "text",
            new WhatsAppTextBody(message)));

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new WhatsAppSendResult(false, $"WhatsApp send failed ({(int)response.StatusCode}): {payload}");
        }

        return new WhatsAppSendResult(true, "Sent");
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits;
    }

    private sealed record WhatsAppTextPayload(
        [property: JsonPropertyName("messaging_product")] string MessagingProduct,
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] WhatsAppTextBody Text);

    private sealed record WhatsAppTextBody(
        [property: JsonPropertyName("body")] string Body);
}
