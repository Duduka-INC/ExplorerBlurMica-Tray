using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// The settings window. Like the flyout, its backdrop follows the material being picked.
/// Closing it only hides it, so an unapplied draft survives until the app exits.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private const int WidthDip = 1040;
    private const int HeightDip = 780;

    private readonly EffectController _effect;
    private readonly nint _handle;
    private Material? _backdropMaterial;
    private bool _exiting;

    public SettingsWindow(EffectController effect)
    {
        InitializeComponent();
        _effect = effect;
        _handle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        Title = Strings.AppName;
        TitleText.Text = Strings.AppName;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 760;
            presenter.PreferredMinimumHeight = 560;
        }
        AppWindow.Closing += (_, e) =>
        {
            if (_exiting)
                return;
            e.Cancel = true;
            AppWindow.Hide();
        };

        NavEffect.Content = Strings.NavEffect;
        NavPresets.Content = Strings.NavPresets;
        NavGeneral.Content = Strings.NavGeneral;
        ComingSoonText.Text = Strings.ComingSoon;
        Nav.SelectedItem = NavEffect;

        EffectView.Attach(effect);
        _effect.Changed += () =>
        {
            if (AppWindow.IsVisible)
                UpdateBackdrop();
        };
    }

    public void ShowAndActivate()
    {
        _effect.Reload();
        if (!AppWindow.IsVisible)
            CenterOnScreen();
        UpdateBackdrop();
        EffectView.Refresh();
        AppWindow.Show();
        Activate();
        SetForegroundWindow(_handle);
    }

    public void CloseForExit()
    {
        _exiting = true;
        Close();
    }

    private void CenterOnScreen()
    {
        double scale = GetDpiForWindow(_handle) / 96.0;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        int width = Math.Min((int)(WidthDip * scale), area.Width);
        int height = Math.Min((int)(HeightDip * scale), area.Height);
        AppWindow.MoveAndResize(new RectInt32(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height));
    }

    private void UpdateBackdrop()
    {
        var material = _effect.Installed ? _effect.Draft.Material : Material.Mica;
        if (_backdropMaterial == material)
            return;
        _backdropMaterial = material;
        SystemBackdrop = Backdrops.For(material);
        Root.Background = SystemBackdrop is null
            ? (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"]
            : new SolidColorBrush(Colors.Transparent);
    }

    private void OnNavigate(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        bool effect = ReferenceEquals(args.SelectedItem, NavEffect);
        EffectView.Visibility = effect ? Visibility.Visible : Visibility.Collapsed;
        ComingSoonText.Visibility = effect ? Visibility.Collapsed : Visibility.Visible;
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hwnd);
}
