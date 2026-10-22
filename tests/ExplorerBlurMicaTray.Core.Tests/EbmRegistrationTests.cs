using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public sealed class EbmRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ebmt-root-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private void AddVersion(string name, bool withDll = true)
    {
        string dir = Path.Combine(_root, name);
        Directory.CreateDirectory(dir);
        if (withDll)
            File.WriteAllText(Path.Combine(dir, EbmConstants.DllFileName), "dll");
    }

    [Fact]
    public void FindsNewestInstalledVersion()
    {
        AddVersion("2.0.1");
        AddVersion("2.10.0");
        AddVersion("2.9.0");

        Assert.Equal(Path.Combine(_root, "2.10.0", EbmConstants.DllFileName), EbmRegistration.FindInstalledDll(_root));
    }

    [Fact]
    public void IgnoresFoldersWithoutDllOrWithOtherNames()
    {
        AddVersion("3.0.0", withDll: false);
        AddVersion("backup");
        AddVersion("2.1.0");

        Assert.Equal(Path.Combine(_root, "2.1.0", EbmConstants.DllFileName), EbmRegistration.FindInstalledDll(_root));
    }

    [Fact]
    public void NothingInstalled()
    {
        Assert.Null(EbmRegistration.FindInstalledDll(_root));
    }
}
