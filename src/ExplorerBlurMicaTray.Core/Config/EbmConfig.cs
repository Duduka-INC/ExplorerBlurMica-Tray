using System.Globalization;
using System.Text;

namespace ExplorerBlurMicaTray.Core;

/// <summary>Values of <c>effect</c> in ExplorerBlurMica's config.ini.</summary>
public enum EbmEffect
{
    /// <summary>Windows 10 and Windows 11 up to 22H2 only.</summary>
    Blur = 0,
    Acrylic = 1,
    Mica = 2,
    BlurClear = 3,
    MicaAlt = 4,
}

/// <summary>A blend color drawn over the effect; <see cref="A"/> is its opacity.</summary>
public readonly record struct BlendColor(byte R, byte G, byte B, byte A);

/// <summary>ExplorerBlurMica's config.ini. Defaults match the file shipped with release 2.1.0.</summary>
public sealed class EbmConfig
{
    public EbmEffect Effect { get; set; } = EbmEffect.Mica;
    public bool ClearAddress { get; set; } = true;
    public bool ClearBarBg { get; set; } = true;
    public bool ClearWinUIBg { get; set; } = true;
    public bool ShowLine { get; set; } = true;
    public BlendColor Light { get; set; } = new(255, 255, 255, 200);
    public BlendColor Dark { get; set; } = new(0, 0, 0, 120);

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static EbmConfig Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>Reads what it can; a missing or malformed value keeps its default.</summary>
    public static EbmConfig Parse(string text)
    {
        var ini = IniDocument.Parse(text);
        var config = new EbmConfig();

        if (int.TryParse(ini.Get("config", "effect"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int effect)
            && Enum.IsDefined((EbmEffect)effect))
            config.Effect = (EbmEffect)effect;
        config.ClearAddress = ReadBool(ini, "clearAddress", config.ClearAddress);
        config.ClearBarBg = ReadBool(ini, "clearBarBg", config.ClearBarBg);
        config.ClearWinUIBg = ReadBool(ini, "clearWinUIBg", config.ClearWinUIBg);
        config.ShowLine = ReadBool(ini, "showLine", config.ShowLine);
        config.Light = ReadColor(ini, "light", config.Light);
        config.Dark = ReadColor(ini, "dark", config.Dark);
        return config;
    }

    /// <summary>Writes these values into <paramref name="existingText"/>, keeping everything else in it.</summary>
    public string ApplyTo(string existingText)
    {
        var ini = IniDocument.Parse(existingText);
        ini.Set("config", "effect", ((int)Effect).ToString(CultureInfo.InvariantCulture));
        ini.Set("config", "clearAddress", Format(ClearAddress));
        ini.Set("config", "clearBarBg", Format(ClearBarBg));
        ini.Set("config", "clearWinUIBg", Format(ClearWinUIBg));
        ini.Set("config", "showLine", Format(ShowLine));
        WriteColor(ini, "light", Light);
        WriteColor(ini, "dark", Dark);
        return ini.ToString();
    }

    /// <summary>
    /// Rewrites the file in place. The installer gives the user write access to this one file,
    /// not to its folder, so a temp-file-and-rename save is not possible.
    /// </summary>
    public void Save(string path)
    {
        string existing = File.Exists(path) ? File.ReadAllText(path) : "";
        File.WriteAllText(path, ApplyTo(existing), Utf8NoBom);
    }

    private static bool ReadBool(IniDocument ini, string key, bool fallback) =>
        bool.TryParse(ini.Get("config", key), out bool value) ? value : fallback;

    private static BlendColor ReadColor(IniDocument ini, string section, BlendColor fallback) =>
        new(
            ReadByte(ini, section, "r", fallback.R),
            ReadByte(ini, section, "g", fallback.G),
            ReadByte(ini, section, "b", fallback.B),
            ReadByte(ini, section, "a", fallback.A));

    private static byte ReadByte(IniDocument ini, string section, string key, byte fallback) =>
        int.TryParse(ini.Get(section, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? (byte)Math.Clamp(value, 0, 255)
            : fallback;

    private static void WriteColor(IniDocument ini, string section, BlendColor color)
    {
        ini.Set(section, "r", color.R.ToString(CultureInfo.InvariantCulture));
        ini.Set(section, "g", color.G.ToString(CultureInfo.InvariantCulture));
        ini.Set(section, "b", color.B.ToString(CultureInfo.InvariantCulture));
        ini.Set(section, "a", color.A.ToString(CultureInfo.InvariantCulture));
    }

    private static string Format(bool value) => value ? "true" : "false";
}
