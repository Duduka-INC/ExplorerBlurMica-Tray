using ExplorerBlurMicaTray.Core.Presets;

namespace ExplorerBlurMicaTray.Core.Tests;

public sealed class PresetFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ebmt-presets-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "presets.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static SavedPreset Sample(string name) =>
        new(name, "Acrylic", new BlendColor(11, 26, 51, 153), new BlendColor(214, 228, 255, 0), true, false, true, false);

    [Fact]
    public void MissingFileMeansNoPresets()
    {
        Assert.Empty(PresetFile.Load(FilePath));
    }

    [Fact]
    public void SavedPresetsLoadBack()
    {
        SavedPreset[] presets = [Sample("Ночной синий"), Sample("Second") with { Material = "Mica", ShowLine = true }];

        PresetFile.Save(FilePath, presets);

        Assert.Equal(presets, PresetFile.Load(FilePath));
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void FileUsesReadableNames()
    {
        PresetFile.Save(FilePath, [Sample("Ночной синий")]);

        string json = File.ReadAllText(FilePath);

        Assert.Contains("\"name\": \"Ночной синий\"", json);
        Assert.Contains("\"version\": 1", json);
        Assert.Contains("\"material\": \"Acrylic\"", json);
        Assert.Contains("\"clearWinUIBg\": true", json);
        Assert.Contains("\"r\": 11", json);
    }

    [Fact]
    public void BrokenFileIsMovedAsideNotLost()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Empty(PresetFile.Load(FilePath));
        Assert.False(File.Exists(FilePath));
        Assert.Equal("{ not json", File.ReadAllText(FilePath + ".broken"));
    }

    [Fact]
    public void PresetsWithoutANameAreSkipped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, """
            { "version": 1, "presets": [
              { "name": " ", "material": "Mica" },
              { "name": "Kept", "material": "Blur", "dark": { "r": 1, "g": 2, "b": 3, "a": 4 } }
            ] }
            """);

        var presets = PresetFile.Load(FilePath);

        var kept = Assert.Single(presets);
        Assert.Equal("Kept", kept.Name);
        Assert.Equal(new BlendColor(1, 2, 3, 4), kept.Dark);
    }
}
