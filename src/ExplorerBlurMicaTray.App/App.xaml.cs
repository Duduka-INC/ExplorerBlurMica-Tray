using Microsoft.UI.Xaml;

namespace ExplorerBlurMicaTray.App;

/// <summary>A tray-only app: no main window, just the tray icon, its flyout and its menu.</summary>
public partial class App : Application
{
    private FlyoutWindow? _flyout;
    private TrayIcon? _tray;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _flyout = new FlyoutWindow();
        _tray = new TrayIcon(_flyout.Handle, Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"), Strings.AppName)
        {
            LeftClick = _flyout.Toggle,
            BuildMenu = BuildMenu,
        };
        _flyout.Prepare();
    }

    private IEnumerable<TrayMenuItem> BuildMenu()
    {
        var flyout = _flyout!;
        if (flyout.CanToggleDefault)
        {
            yield return new TrayMenuItem(Strings.MenuDefault, flyout.ToggleDefaultFromMenu, flyout.IsDefaultApplied);
            yield return new TrayMenuItem(Strings.MenuReopen, flyout.ReopenExplorerWindows);
            yield return TrayMenuItem.Separator;
        }
        yield return new TrayMenuItem(Strings.MenuExit, Quit);
    }

    private void Quit()
    {
        _tray?.Dispose();
        _flyout?.CloseForExit();
        Exit();
    }
}
