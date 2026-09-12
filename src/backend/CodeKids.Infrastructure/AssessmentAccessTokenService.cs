using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CodeKids.Infrastructure;

/// <summary>
/// Compact URL-safe signed keys: version + kind + resource + student + expiry + truncated HMAC.
/// ~62 chars base64url — suitable for /go/{key} links.
/// </summary>
public sealed class AssessmentAccessTokenService(IConfiguration configuration) : IAssessmentAccessTokenService
{
    private const byte Version = 1;
    private const int MacLength = 8;
    private const int PayloadLength = 1 + 1 + 16 + 16 + 4; // 38
    private const int TokenLength = PayloadLength + MacLength; // 46

    public string CreateToken(AssessmentLinkKind kind, Guid resourceId, Guid studentId, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        var expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        if (expires > uint.MaxValue)
        {
            expires = uint.MaxValue;
        }

        Span<byte> payload = stackalloc byte[PayloadLength];
        payload[0] = Version;
        payload[1] = (byte)kind;
        resourceId.TryWriteBytes(payload[2..18]);
        studentId.TryWriteBytes(payload[18..34]);
        BinaryPrimitives.WriteUInt32BigEndian(payload[34..38], (uint)expires);

        Span<byte> token = stackalloc byte[TokenLength];
        payload.CopyTo(token);
        ComputeMac(payload, token[PayloadLength..]);

        return Base64UrlEncode(token);
    }

    public bool TryValidate(
        string token,
        out AssessmentLinkKind kind,
        out Guid resourceId,
        out Guid studentId,
        out DateTimeOffset expiresAt)
    {
        kind = default;
        resourceId = Guid.Empty;
        studentId = Guid.Empty;
        expiresAt = default;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[TokenLength];
        if (!Base64UrlDecode(token.Trim(), bytes))
        {
            return false;
        }

        if (bytes[0] != Version)
        {
            return false;
        }

        var kindByte = bytes[1];
        if (kindByte is < (byte)AssessmentLinkKind.Exam or > (byte)AssessmentLinkKind.Assignment)
        {
            return false;
        }

        Span<byte> expectedMac = stackalloc byte[MacLength];
        ComputeMac(bytes[..PayloadLength], expectedMac);
        if (!CryptographicOperations.FixedTimeEquals(expectedMac, bytes[PayloadLength..]))
        {
            return false;
        }

        kind = (AssessmentLinkKind)kindByte;
        resourceId = new Guid(bytes[2..18]);
        studentId = new Guid(bytes[18..34]);
        var expiresUnix = BinaryPrimitives.ReadUInt32BigEndian(bytes[34..38]);
        expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
        return expiresAt >= DateTimeOffset.UtcNow;
    }

    private void ComputeMac(ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "CodeKids-Assessment-Link-Dev-Key");
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(key, payload, hash);
        hash[..MacLength].CopyTo(destination);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool Base64UrlDecode(string input, Span<byte> destination)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
            case 1: return false;
        }

        try
        {
            var decoded = Convert.FromBase64String(padded);
            if (decoded.Length != destination.Length)
            {
                return false;
            }

            decoded.CopyTo(destination);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
