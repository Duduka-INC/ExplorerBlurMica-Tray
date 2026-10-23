using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExplorerBlurMicaTray.Core.Presets;

/// <summary>A preset the user saved: everything the app can set for Explorer, under a name.</summary>
/// <param name="Material">The app's material name (Default, Mica, MicaAlt, Acrylic, Blur).</param>
public sealed record SavedPreset(
    string Name,
    string Material,
    BlendColor Dark,
    BlendColor Light,
    bool ClearAddress,
    bool ClearBarBg,
    bool ClearWinUIBg,
    bool ShowLine);

/// <summary>The user's presets, kept as JSON next to the app's other per-user data.</summary>
public static class PresetFile
{
    // Names stay readable in the file (no \u escapes); it is only ever read back as JSON.
    private static readonly PresetJsonContext Json = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    public static string DefaultPath => Path.Combine(AppPaths.DataRoot, "presets.json");

    /// <summary>
    /// No file means no presets. A file that can't be read is moved aside to presets.json.broken
    /// rather than overwritten by the next save, so the user's presets can still be recovered.
    /// </summary>
    public static List<SavedPreset> Load(string path)
    {
        if (!File.Exists(path))
            return [];
        try
        {
            var document = JsonSerializer.Deserialize(File.ReadAllBytes(path), Json.PresetDocument);
            return document?.Presets?.Where(p => !string.IsNullOrWhiteSpace(p?.Name) && p.Material is not null).ToList() ?? [];
        }
        catch (JsonException)
        {
            File.Move(path, path + ".broken", overwrite: true);
            return [];
        }
    }

    /// <summary>Writes a temporary file first, so a crash mid-write can't leave a half-written file.</summary>
    public static void Save(string path, IReadOnlyList<SavedPreset> presets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllBytes(temp, JsonSerializer.SerializeToUtf8Bytes(new PresetDocument(1, [.. presets]), Json.PresetDocument));
        File.Move(temp, path, overwrite: true);
    }
}

internal sealed record PresetDocument(int Version, List<SavedPreset>? Presets);

[JsonSerializable(typeof(PresetDocument))]
internal sealed partial class PresetJsonContext : JsonSerializerContext;
