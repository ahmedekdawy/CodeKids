using System.Text;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Normalizes phone numbers into the Baileys JID format.
/// 01xxxxxxxxx    -> 201xxxxxxxxx@s.whatsapp.net (Egyptian local)
/// +201xxxxxxxxx  -> 201xxxxxxxxx@s.whatsapp.net
/// 00971xxxxxxxxx -> 971xxxxxxxxx@s.whatsapp.net
/// 5 / group:5 / 1203…@g.us → WhatsApp group destination
/// </summary>
public static class WhatsAppPhone
{
    public const string JidSuffix = "@s.whatsapp.net";
    public const string GroupJidSuffix = "@g.us";

    public static string? ToJid(string? phone)
    {
        if (!TryParseTarget(phone, out var target))
        {
            return null;
        }

        return target.IsGroup
            ? target.GroupJid
            : target.PhoneDigits + JidSuffix;
    }

    public static bool TryParseTarget(string? input, out WhatsAppTarget target)
    {
        target = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["group:".Length..].Trim();
        }
        else if (trimmed.StartsWith("g:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].Trim();
        }

        var at = trimmed.IndexOf('@');
        if (at >= 0 && trimmed.AsSpan(at).Equals(GroupJidSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var idPart = trimmed[..at];
            if (!long.TryParse(DigitsOnly(idPart), out var groupId) || groupId <= 0)
            {
                return false;
            }

            target = WhatsAppTarget.Group(groupId, groupId + GroupJidSuffix);
            return true;
        }

        var rawDigits = DigitsOnly(trimmed);
        if (rawDigits.Length is > 0 and < 8 && long.TryParse(rawDigits, out var shortGroupId) && shortGroupId > 0)
        {
            target = WhatsAppTarget.Group(shortGroupId, shortGroupId + GroupJidSuffix);
            return true;
        }

        var phoneDigits = ToDigits(trimmed);
        if (phoneDigits is null)
        {
            return false;
        }

        target = WhatsAppTarget.Phone(phoneDigits);
        return true;
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

    private static string DigitsOnly(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}

public readonly record struct WhatsAppTarget(bool IsGroup, string PhoneDigits, long GroupId, string GroupJid)
{
    public static WhatsAppTarget Phone(string digits) => new(false, digits, 0, string.Empty);

    public static WhatsAppTarget Group(long groupId, string groupJid) => new(true, string.Empty, groupId, groupJid);
}
