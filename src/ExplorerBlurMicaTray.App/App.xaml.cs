using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace ExplorerBlurMicaTray.App;

/// <summary>A tray app: the tray icon, its flyout and menu, and the settings window on demand.</summary>
public partial class App : Application
{
    private readonly EffectController _effect = new();
    private FlyoutWindow? _flyout;
    private SettingsWindow? _settings;
    private TrayIcon? _tray;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _flyout = new FlyoutWindow(_effect);
        _flyout.SettingsRequested += ShowSettings;
        _tray = new TrayIcon(_flyout.Handle, Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"), Strings.AppName)
        {
            LeftClick = () =>
            {
                if (AppSettings.LeftClickOpensSettings)
                    ShowSettings();
                else
                    _flyout.Toggle();
            },
            BuildMenu = BuildMenu,
        };
        _flyout.Prepare();
    }

    private void ShowSettings()
    {
        // The window is created on first use and kept after it closes; it only hides.
        _settings ??= new SettingsWindow(_effect);
        _settings.ShowAndActivate();
    }

    private IEnumerable<TrayMenuItem> BuildMenu()
    {
        yield return new TrayMenuItem(Strings.OpenSettings, ShowSettings);
        if (_effect.Installed && !_effect.Busy)
        {
            yield return new TrayMenuItem(Strings.MenuDefault, () => _ = _effect.ToggleDefaultAsync(),
                _effect.Applied.Material == Material.Default);
            yield return new TrayMenuItem(Strings.MenuReopen, () => _ = _effect.ReopenWindowsAsync());
        }
        yield return TrayMenuItem.Separator;
        yield return new TrayMenuItem(Strings.MenuExit, Quit);
    }

    /// <summary>Starts a new instance, which waits for this one to exit, and exits. Used to switch the language.</summary>
    public void Restart()
    {
        if (Environment.ProcessPath is { } exe)
            Process.Start(new ProcessStartInfo(exe, Program.RestartArgument) { UseShellExecute = false })?.Dispose();
        Quit();
    }

    private void Quit()
    {
        _tray?.Dispose();
        _settings?.CloseForExit();
        _flyout?.CloseForExit();
        _effect.Dispose();
        Exit();
    }
}
