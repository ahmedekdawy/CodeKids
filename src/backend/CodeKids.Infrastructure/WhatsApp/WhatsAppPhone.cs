using System.Text;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Normalizes phone numbers into the Baileys JID format.
/// 01xxxxxxxxx    -> 201xxxxxxxxx@s.whatsapp.net (Egyptian local)
/// +201xxxxxxxxx  -> 201xxxxxxxxx@s.whatsapp.net
/// 00971xxxxxxxxx -> 971xxxxxxxxx@s.whatsapp.net
/// </summary>
public static class WhatsAppPhone
{
    public const string JidSuffix = "@s.whatsapp.net";

    public static string? ToJid(string? phone)
    {
        var digits = ToDigits(phone);
        return digits is null ? null : digits + JidSuffix;
    }

    public static string? ToDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var builder = new StringBuilder(phone.Length);
        foreach (var c in phone)
        {
            if (char.IsDigit(c))
            {
                builder.Append(c);
            }
        }

        var cleaned = builder.ToString();

        // Strip the international dialing prefix.
        if (cleaned.StartsWith("00", StringComparison.Ordinal))
        {
            cleaned = cleaned[2..];
        }

        // Egyptian local format needs the country code prepended.
        if (cleaned.StartsWith('0') && cleaned.Length is 10 or 11)
        {
            cleaned = "2" + cleaned;
        }

        return cleaned.Length < 8 ? null : cleaned;
    }
}
