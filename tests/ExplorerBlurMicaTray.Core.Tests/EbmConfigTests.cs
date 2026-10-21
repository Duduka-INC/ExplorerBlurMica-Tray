using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public class EbmConfigTests
{
    // config.ini shipped in Release_x64.zip of ExplorerBlurMica 2.1.0.
    private const string Shipped =
        "[config]\r\neffect=2\r\nclearAddress=true\r\nclearBarBg=true\r\nclearWinUIBg=true\r\nshowLine=true\r\n" +
        "[light]\r\nr=255\r\ng=255\r\nb=255\r\na=200\r\n[dark]\r\nr=0\r\ng=0\r\nb=0\r\na=120\r\n";

    [Fact]
    public void ParsesShippedConfig()
    {
        var config = EbmConfig.Parse(Shipped);

        Assert.Equal(EbmEffect.Mica, config.Effect);
        Assert.True(config.ClearAddress);
        Assert.True(config.ShowLine);
        Assert.Equal(new BlendColor(255, 255, 255, 200), config.Light);
        Assert.Equal(new BlendColor(0, 0, 0, 120), config.Dark);
    }

    [Fact]
    public void UnchangedConfigWritesBackTheSameText()
    {
        Assert.Equal(Shipped, EbmConfig.Parse(Shipped).ApplyTo(Shipped));
    }

    [Fact]
    public void ApplyToChangesOnlyItsValues()
    {
        var config = EbmConfig.Parse(Shipped);
        config.Effect = EbmEffect.Acrylic;
        config.ShowLine = false;
        config.Dark = config.Dark with { A = 47 };

        var reread = EbmConfig.Parse(config.ApplyTo(Shipped + "; kept\r\n"));

        Assert.Equal(EbmEffect.Acrylic, reread.Effect);
        Assert.False(reread.ShowLine);
        Assert.Equal(47, reread.Dark.A);
        Assert.Contains("; kept", config.ApplyTo(Shipped + "; kept\r\n"));
    }

    [Theory]
    [InlineData("[config]\neffect=9\n")]
    [InlineData("[config]\neffect=mica\n")]
    [InlineData("")]
    public void BadOrMissingEffectKeepsDefault(string text)
    {
        Assert.Equal(EbmEffect.Mica, EbmConfig.Parse(text).Effect);
    }

    [Fact]
    public void ColorComponentsAreClamped()
    {
        var config = EbmConfig.Parse("[dark]\nr=-5\ng=300\nb=x\na=128\n");

        Assert.Equal(new BlendColor(0, 255, 0, 128), config.Dark);
    }

    [Fact]
    public void SaveRewritesFileInPlace()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ebmt-config-{Guid.NewGuid():N}.ini");
        try
        {
            File.WriteAllText(path, Shipped);
            var config = EbmConfig.Load(path);
            config.Effect = EbmEffect.MicaAlt;
            config.Save(path);

            Assert.Equal(EbmEffect.MicaAlt, EbmConfig.Load(path).Effect);
            Assert.NotEqual(0xEF, File.ReadAllBytes(path)[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
