using Microsoft.Win32;

namespace ExplorerBlurMicaTray.Core.Tests;

/// <summary>Runs against a throwaway key under HKCU standing in for the user's hive.</summary>
public sealed class AutostartTests : IDisposable
{
    private const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string Parent = @"Software\ExplorerBlurMica-Tray-Tests";
    private readonly string _rootPath = Parent + @"\" + Guid.NewGuid().ToString("N");
    private readonly RegistryKey _root;
    private readonly Autostart _autostart;

    public AutostartTests()
    {
        _root = Registry.CurrentUser.CreateSubKey(_rootPath);
        _autostart = new Autostart(_root);
    }

    public void Dispose()
    {
        _root.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_rootPath, throwOnMissingSubKey: false);
        // Tests run in parallel; the last one out removes the parent.
        using var parent = Registry.CurrentUser.OpenSubKey(Parent);
        if (parent is { SubKeyCount: 0 })
        {
            try
            {
                Registry.CurrentUser.DeleteSubKey(Parent, throwOnMissingSubKey: false);
            }
            catch (InvalidOperationException)
            {
                // Another test created its key meanwhile; it cleans up.
            }
        }
    }

    [Fact]
    public void OffByDefault()
    {
        Assert.False(_autostart.IsEnabled);
    }

    [Fact]
    public void EnableWritesQuotedPath()
    {
        _autostart.Enable(@"C:\Program Files\ExplorerBlurMica-Tray\ExplorerBlurMicaTray.exe");

        Assert.True(_autostart.IsEnabled);
        using var run = _root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        Assert.Equal("\"C:\\Program Files\\ExplorerBlurMica-Tray\\ExplorerBlurMicaTray.exe\"", run!.GetValue(Autostart.ValueName));
    }

    [Fact]
    public void SwitchedOffInStartupAppsCountsAsOff()
    {
        _autostart.Enable(@"C:\app.exe");
        using (var approved = _root.CreateSubKey(Approved))
            approved.SetValue(Autostart.ValueName, new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);

        Assert.False(_autostart.IsEnabled);
    }

    [Fact]
    public void EnableClearsTheStartupAppsMark()
    {
        using (var approved = _root.CreateSubKey(Approved))
            approved.SetValue(Autostart.ValueName, new byte[] { 3, 0, 0, 0 }, RegistryValueKind.Binary);

        _autostart.Enable(@"C:\app.exe");

        Assert.True(_autostart.IsEnabled);
    }

    [Fact]
    public void AllowedInStartupAppsStaysOn()
    {
        _autostart.Enable(@"C:\app.exe");
        using (var approved = _root.CreateSubKey(Approved))
            approved.SetValue(Autostart.ValueName, new byte[] { 2, 0, 0, 0 }, RegistryValueKind.Binary);

        Assert.True(_autostart.IsEnabled);
    }

    [Fact]
    public void DisableRemovesTheEntry()
    {
        _autostart.Enable(@"C:\app.exe");

        _autostart.Disable();

        Assert.False(_autostart.IsEnabled);
        using var run = _root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        Assert.Null(run!.GetValue(Autostart.ValueName));
    }
}
