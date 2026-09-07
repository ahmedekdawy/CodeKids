using System.Security.Cryptography;
using System.Text;

namespace CodeKids.Infrastructure.WhatsApp;

/// <summary>
/// Encrypts request bodies in the CryptoJS default format, which is what the whats-pro
/// gateway expects: AES-256-CBC with the key and IV derived from the passphrase through
/// OpenSSL's EVP_BytesToKey, emitted as base64 of "Salted__" + salt + ciphertext. The
/// derivation uses MD5 because that format requires it, not as a security choice.
/// </summary>
public static class CryptoJsAes
{
    private const int KeyLength = 32;
    private const int IvLength = 16;
    private const int SaltLength = 8;

    private static readonly byte[] SaltedPrefix = "Salted__"u8.ToArray();

    public static string Encrypt(string data, string passphrase, byte[]? salt = null)
    {
        salt ??= RandomNumberGenerator.GetBytes(SaltLength);

        var derived = DeriveKeyAndIv(Encoding.UTF8.GetBytes(passphrase), salt);
        var key = derived.AsSpan(0, KeyLength).ToArray();
        var iv = derived.AsSpan(KeyLength, IvLength).ToArray();

        using var aes = Aes.Create();
        aes.Key = key;
        var cipherText = aes.EncryptCbc(Encoding.UTF8.GetBytes(data), iv, PaddingMode.PKCS7);

        var payload = new byte[SaltedPrefix.Length + salt.Length + cipherText.Length];
        SaltedPrefix.CopyTo(payload, 0);
        salt.CopyTo(payload, SaltedPrefix.Length);
        cipherText.CopyTo(payload, SaltedPrefix.Length + salt.Length);

        return Convert.ToBase64String(payload);
    }

    /// <summary>Repeats D(i) = MD5(D(i-1) + passphrase + salt) until 48 bytes are available.</summary>
    private static byte[] DeriveKeyAndIv(byte[] passphrase, byte[] salt)
    {
        var derived = new byte[KeyLength + IvLength];
        var block = Array.Empty<byte>();
        var filled = 0;

        while (filled < derived.Length)
        {
            var buffer = new byte[block.Length + passphrase.Length + salt.Length];
            block.CopyTo(buffer, 0);
            passphrase.CopyTo(buffer, block.Length);
            salt.CopyTo(buffer, block.Length + passphrase.Length);

            block = MD5.HashData(buffer);
            var take = Math.Min(block.Length, derived.Length - filled);
            block.AsSpan(0, take).CopyTo(derived.AsSpan(filled));
            filled += take;
        }

        return derived;
    }
}
