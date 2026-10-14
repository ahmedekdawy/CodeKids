using System.Collections.Concurrent;
using SkiaSharp;

namespace CodeKids.Api;

/// <summary>
/// Turns a tenant's uploaded logo, whatever its shape, into the square PNG icons an installed app needs.
/// The logo is fitted whole onto a background taken from its own corners, so wide logos are not cropped.
/// </summary>
public static class TenantIconRenderer
{
    public static readonly int[] Sizes = [180, 192, 512];

    /// <summary>Share of the icon the logo may fill. Maskable icons are cropped to a circle by Android, so they keep to its safe zone.</summary>
    private const float AnyShare = 1f;
    private const float MaskableShare = 0.72f;

    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    public static async Task<byte[]?> RenderAsync(string cacheKey, Func<Task<byte[]>> readLogo, int size, bool maskable)
    {
        var key = $"{cacheKey}|{size}|{maskable}";
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        using var logo = SKBitmap.Decode(await readLogo());
        if (logo is null || logo.Width == 0 || logo.Height == 0)
        {
            return null;
        }

        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(BackgroundOf(logo));

        var share = maskable ? MaskableShare : AnyShare;
        var scale = Math.Min(size * share / logo.Width, size * share / logo.Height);
        var width = logo.Width * scale;
        var height = logo.Height * scale;
        var destination = SKRect.Create((size - width) / 2, (size - height) / 2, width, height);

        using var image = SKImage.FromBitmap(logo);
        canvas.DrawImage(image, destination, new SKSamplingOptions(SKCubicResampler.Mitchell));

        using var snapshot = surface.Snapshot();
        using var png = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();

        // A handful of tenants times three sizes; clearing now and then keeps replaced logos from piling up.
        if (Cache.Count > 200)
        {
            Cache.Clear();
        }

        Cache[key] = bytes;
        return bytes;
    }

    /// <summary>The average of the logo's corners, or white when they are transparent.</summary>
    private static SKColor BackgroundOf(SKBitmap logo)
    {
        var corners = new[]
        {
            logo.GetPixel(0, 0),
            logo.GetPixel(logo.Width - 1, 0),
            logo.GetPixel(0, logo.Height - 1),
            logo.GetPixel(logo.Width - 1, logo.Height - 1)
        };

        if (corners.Average(c => c.Alpha) < 128)
        {
            return SKColors.White;
        }

        return new SKColor(
            (byte)corners.Average(c => c.Red),
            (byte)corners.Average(c => c.Green),
            (byte)corners.Average(c => c.Blue));
    }
}
