using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.App;

/// <summary>What the user picks in the flyout. Default means ExplorerBlurMica is switched off.</summary>
public enum Material
{
    Default,
    Mica,
    MicaAlt,
    Acrylic,
    Blur,
}

/// <summary>A built-in preset; <see cref="TintPercent"/> is null where the tint does not apply.</summary>
public sealed record Preset(string Name, Material Material, int? TintPercent);

internal static class Materials
{
    public static readonly Material[] All = [Material.Default, Material.Mica, Material.MicaAlt, Material.Acrylic, Material.Blur];

    public static string Label(Material material) => material switch
    {
        Material.Default => "Default",
        Material.Mica => "Mica",
        Material.MicaAlt => "Mica Alt",
        Material.Acrylic => "Acrylic",
        Material.Blur => "Blur",
        _ => material.ToString(),
    };

    public static readonly Preset[] BuiltInPresets =
    [
        new("Default", Material.Default, null),
        new(Strings.PresetGlass, Material.Acrylic, 47),
        new("Mica", Material.Mica, null),
        new(Strings.PresetDeep, Material.MicaAlt, null),
        new(Strings.PresetClearBlur, Material.Blur, null),
    ];

    /// <summary>
    /// ExplorerBlurMica draws its blend color only for Acrylic: Blur (Clear) is untinted by design,
    /// and Mica goes through a private DWM attribute that takes no color.
    /// </summary>
    public static bool TintApplies(Material material) => material == Material.Acrylic;

    public static Material FromEffect(EbmEffect effect) => effect switch
    {
        EbmEffect.Mica => Material.Mica,
        EbmEffect.MicaAlt => Material.MicaAlt,
        EbmEffect.Acrylic => Material.Acrylic,
        // Plain Blur only works up to Windows 11 22H2; the app always writes Blur (Clear).
        EbmEffect.Blur or EbmEffect.BlurClear => Material.Blur,
        _ => Material.Acrylic,
    };

    public static EbmEffect ToEffect(Material material) => material switch
    {
        Material.Mica => EbmEffect.Mica,
        Material.MicaAlt => EbmEffect.MicaAlt,
        Material.Blur => EbmEffect.BlurClear,
        _ => EbmEffect.Acrylic,
    };

    public static int AlphaToPercent(byte alpha) => (int)Math.Round(alpha / 255.0 * 100);

    public static byte PercentToAlpha(int percent) => (byte)Math.Round(Math.Clamp(percent, 0, 100) / 100.0 * 255);
}
