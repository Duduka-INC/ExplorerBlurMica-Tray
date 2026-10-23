using ExplorerBlurMicaTray.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Windows.UI;

namespace ExplorerBlurMicaTray.App;

/// <summary>What the Effect preview and the preset thumbnails draw: the wallpaper and each material's look.</summary>
internal static class PreviewArt
{
    private static Task<BitmapImage?>? _wallpaper;

    /// <summary>
    /// The window background a material gives Explorer, drawn with in-app acrylic, which blurs
    /// whatever is behind it in our window: Acrylic and Blur show the window behind, Mica only the
    /// wallpaper copy placed under it.
    /// </summary>
    public static Brush Backdrop(EffectSettings settings)
    {
        Color solid = SystemEffects.IsAppsLightTheme() ? ColorHelper.FromArgb(255, 243, 243, 243) : ColorHelper.FromArgb(255, 32, 32, 32);
        return settings.Material switch
        {
            Material.Default => new SolidColorBrush(solid),
            Material.Acrylic => Acrylic(settings.CurrentBlend, solid),
            Material.Blur => new AcrylicBrush { TintColor = solid, TintOpacity = 0, TintLuminosityOpacity = 0, FallbackColor = solid },
            Material.MicaAlt => new AcrylicBrush { TintColor = solid, TintOpacity = 0.45, TintLuminosityOpacity = 0.3, FallbackColor = solid },
            _ => new AcrylicBrush { TintColor = solid, TintOpacity = 0.75, TintLuminosityOpacity = 0.6, FallbackColor = solid },
        };
    }

    /// <summary>
    /// ExplorerBlurMica keeps the system acrylic when the blend color is fully transparent, and a low
    /// density looks almost the same in Explorer. So the preview starts from the system tint and
    /// moves toward the blend color as the density grows, without a jump between 0% and 1%.
    /// </summary>
    private static AcrylicBrush Acrylic(BlendColor blend, Color solid)
    {
        double t = blend.A / 255.0;
        byte Mix(byte from, byte to) => (byte)Math.Round(from + (to - from) * t);
        return new AcrylicBrush
        {
            TintColor = ColorHelper.FromArgb(255, Mix(solid.R, blend.R), Mix(solid.G, blend.G), Mix(solid.B, blend.B)),
            TintOpacity = 0.6 + 0.3 * t,
            FallbackColor = solid,
        };
    }

    /// <summary>The current wallpaper, loaded once and shared; null when it can't be read.</summary>
    public static Task<BitmapImage?> WallpaperAsync() => _wallpaper ??= LoadWallpaperAsync();

    private static async Task<BitmapImage?> LoadWallpaperAsync()
    {
        if (FindWallpaper() is not { } path)
            return null;
        try
        {
            var bitmap = new BitmapImage { DecodePixelWidth = 1280 };
            using var file = File.OpenRead(path);
            using var stream = file.AsRandomAccessStream();
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
        catch (Exception)
        {
            // Without a readable wallpaper the previews keep their plain background.
            return null;
        }
    }

    /// <summary>Windows keeps a copy of the current wallpaper, slideshows and Spotlight included.</summary>
    private static string? FindWallpaper()
    {
        string transcoded = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
        if (File.Exists(transcoded))
            return transcoded;
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        return key?.GetValue("WallPaper") is string { Length: > 0 } path && File.Exists(path) ? path : null;
    }
}
