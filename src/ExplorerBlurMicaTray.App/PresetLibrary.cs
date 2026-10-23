using ExplorerBlurMicaTray.Core.Presets;

namespace ExplorerBlurMicaTray.App;

/// <summary>The built-in presets and the user's own, which are kept in presets.json.</summary>
public sealed class PresetLibrary
{
    private readonly string _path;
    private readonly List<CustomPreset> _custom;

    public PresetLibrary(string path)
    {
        _path = path;
        _custom = Load(path);
    }

    /// <summary>Raised after a preset is added, renamed or deleted.</summary>
    public event Action? Changed;

    public IReadOnlyList<CustomPreset> Custom => _custom;

    public IEnumerable<Preset> All => Materials.BuiltInPresets.Concat<Preset>(_custom);

    /// <summary>The first preset that looks like <paramref name="settings"/>, built-in ones first.</summary>
    public Preset? Match(EffectSettings settings) => All.FirstOrDefault(p => p.Matches(settings));

    /// <summary>Why <paramref name="name"/> can't be used, or null if it can.</summary>
    public string? Check(string name, Preset? renaming = null)
    {
        name = name.Trim();
        if (name.Length == 0)
            return Strings.PresetNameEmpty;
        bool taken = All.Any(p => !ReferenceEquals(p, renaming) && string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
        return taken ? Strings.PresetNameTaken : null;
    }

    /// <exception cref="IOException">The file could not be written; nothing changes then.</exception>
    public void Add(string name, EffectSettings settings) =>
        Update([.. _custom, new CustomPreset(name.Trim(), settings)]);

    public void Rename(CustomPreset preset, string name) =>
        Update(_custom.Select(p => ReferenceEquals(p, preset) ? p with { Name = name.Trim() } : p).ToList());

    public void Delete(CustomPreset preset) =>
        Update(_custom.Where(p => !ReferenceEquals(p, preset)).ToList());

    private void Update(List<CustomPreset> presets)
    {
        try
        {
            PresetFile.Save(_path, presets.Select(ToSaved).ToList());
        }
        catch (UnauthorizedAccessException e)
        {
            throw new IOException(e.Message, e);
        }
        _custom.Clear();
        _custom.AddRange(presets);
        Changed?.Invoke();
    }

    private static List<CustomPreset> Load(string path)
    {
        try
        {
            return PresetFile.Load(path).Select(FromSaved).OfType<CustomPreset>().ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Unreadable right now: start without custom presets and leave the file alone.
            return [];
        }
    }

    private static CustomPreset? FromSaved(SavedPreset saved) =>
        Enum.TryParse(saved.Material, ignoreCase: true, out Material material)
            ? new CustomPreset(saved.Name, new EffectSettings(material, saved.Dark, saved.Light,
                saved.ClearAddress, saved.ClearBarBg, saved.ClearWinUIBg, saved.ShowLine))
            : null;

    private static SavedPreset ToSaved(CustomPreset preset)
    {
        var s = preset.Settings;
        return new SavedPreset(preset.Name, s.Material.ToString(), s.Dark, s.Light,
            s.ClearAddress, s.ClearBarBg, s.ClearWinUIBg, s.ShowLine);
    }
}
