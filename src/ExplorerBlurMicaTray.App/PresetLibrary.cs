using ExplorerBlurMicaTray.Core.Presets;

namespace ExplorerBlurMicaTray.App;

/// <summary>The built-in presets and the user's own, which are kept in presets.json.</summary>
public sealed class PresetLibrary
{
    private readonly string _path;
    private readonly List<CustomPreset> _custom;
    private string? _selectedKey = AppSettings.SelectedPreset;

    public PresetLibrary(string path)
    {
        _path = path;
        _custom = Load(path);
    }

    /// <summary>Raised after a preset is added, renamed, deleted or picked.</summary>
    public event Action? Changed;

    public IReadOnlyList<CustomPreset> Custom => _custom;

    public IEnumerable<Preset> All => Materials.BuiltInPresets.Concat<Preset>(_custom);

    /// <summary>
    /// The one preset to show as current for <paramref name="settings"/>. Several presets can give
    /// the same look (a saved Blur look and the built-in Clear blur, say), so the one the user picked
    /// last wins while it still gives this look. Otherwise, such as after editing by hand, the first
    /// that matches: the user's own come first, as they match every setting, while a built-in one
    /// matches any look with its material (and density).
    /// </summary>
    public Preset? Current(EffectSettings settings)
    {
        if (All.FirstOrDefault(p => p.Key == _selectedKey) is { } selected && selected.Matches(settings))
            return selected;
        return (Preset?)_custom.FirstOrDefault(p => p.Matches(settings)) ?? Materials.BuiltInPresets.FirstOrDefault(p => p.Matches(settings));
    }

    /// <summary>Remembers <paramref name="preset"/> as the user's pick, also across restarts.</summary>
    public void Select(Preset preset)
    {
        SetSelected(preset.Key);
        Changed?.Invoke();
    }

    private void SetSelected(string? key)
    {
        if (key == _selectedKey)
            return;
        _selectedKey = key;
        AppSettings.SelectedPreset = key;
    }

    /// <summary>Why <paramref name="name"/> can't be used, or null if it can.</summary>
    public string? Check(string name, Preset? renaming = null)
    {
        name = name.Trim();
        if (name.Length == 0)
            return Strings.PresetNameEmpty;
        bool taken = All.Any(p => !ReferenceEquals(p, renaming) && string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
        return taken ? Strings.PresetNameTaken : null;
    }

    /// <summary>Saves the settings as a new preset, which becomes the user's pick.</summary>
    /// <exception cref="IOException">The file could not be written; nothing changes then.</exception>
    public void Add(string name, EffectSettings settings)
    {
        var preset = new CustomPreset(name.Trim(), settings);
        Update([.. _custom, preset], preset.Key);
    }

    public void Rename(CustomPreset preset, string name)
    {
        var renamed = preset with { Name = name.Trim() };
        Update(_custom.Select(p => ReferenceEquals(p, preset) ? renamed : p).ToList(),
            _selectedKey == preset.Key ? renamed.Key : _selectedKey);
    }

    public void Delete(CustomPreset preset) =>
        Update(_custom.Where(p => !ReferenceEquals(p, preset)).ToList(),
            _selectedKey == preset.Key ? null : _selectedKey);

    private void Update(List<CustomPreset> presets, string? selectedKey)
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
        SetSelected(selectedKey);
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
