using System.Diagnostics;
using System.Runtime.InteropServices;
using ExplorerBlurMicaTray.Core;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// The flyout above the tray icon. Picking a material changes this window's own backdrop at once;
/// Explorer changes only on Apply, because applying reopens or restarts Explorer.
/// </summary>
public sealed partial class FlyoutWindow : Window
{
    private const int WidthDip = 320;
    private const int MarginDip = 12;
    // A click on the tray icon first deactivates (and hides) the flyout; ignore the reopen that follows.
    private const long ReopenGuardMs = 300;

    private static readonly SolidColorBrush ActiveDot = new(ColorHelper.FromArgb(255, 108, 203, 95));
    private static readonly SolidColorBrush IdleDot = new(ColorHelper.FromArgb(255, 128, 128, 128));
    private static readonly SolidColorBrush PendingDot = new(ColorHelper.FromArgb(255, 96, 205, 255));

    private readonly HttpClient _http = EbmReleaseClient.CreateHttpClient();
    private readonly EbmInstaller _installer;
    private readonly ToggleButton[] _materialButtons;
    private readonly RadioMenuFlyoutItem[] _presetItems;
    private Windows.UI.Color _blendColor = ColorHelper.FromArgb(255, 0, 0, 0);

    private EbmStatus _status = new(EbmState.NotInstalled, null, null);
    private Material _applied = Material.Default;
    private Material _selected = Material.Default;
    private Material _lastEffect = Material.Acrylic;
    private Material? _backdropMaterial;
    private int _appliedTint = 47;
    private int _selectedTint = 47;
    private int _windowCount;
    private bool _busy;
    private bool _updating;
    private bool _preparing;
    private bool _exiting;
    private long _hiddenAt;

    public nint Handle { get; }

    public FlyoutWindow()
    {
        InitializeComponent();
        _installer = new EbmInstaller(new EbmReleaseClient(_http));
        Handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Title = Strings.AppName;

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        // A border without a title bar keeps the rounded corners and shadow of system flyouts.
        presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        AppWindow.Closing += (_, e) =>
        {
            if (_exiting)
                return;
            e.Cancel = true;
            HideFlyout();
        };
        Activated += OnActivated;
        Root.SizeChanged += (_, _) => FitToContent();
        ErrorBar.Closed += (_, _) => ErrorBar.Visibility = Visibility.Collapsed;
        SetOpen(ErrorBar, false);

        TitleText.Text = Strings.FlyoutTitle;
        TransparencyBar.Title = Strings.TransparencyOffTitle;
        TransparencyBar.Message = Strings.TransparencyOffMessage;
        ColorSettingsButton.Content = Strings.OpenWindowsSettings;
        InstallTitleText.Text = Strings.InstallTitle;
        InstallBodyText.Text = Strings.InstallBody;
        InstallNoteText.Text = Strings.InstallNote;
        _presetItems = Materials.BuiltInPresets.Select(preset =>
        {
            var item = new RadioMenuFlyoutItem { Text = preset.Name, GroupName = "presets" };
            item.Click += (_, _) => OnPresetPicked(preset);
            PresetMenu.Items.Add(item);
            return item;
        }).ToArray();

        _materialButtons = new ToggleButton[Materials.All.Length];
        for (int i = 0; i < Materials.All.Length; i++)
        {
            var material = Materials.All[i];
            var button = new ToggleButton
            {
                Content = Materials.Label(material),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 28,
                Padding = new Thickness(0),
                FontSize = 12,
            };
            // Checked/Unchecked, not Click, so keyboard and UI Automation toggles count too.
            button.Checked += (_, _) =>
            {
                if (!_updating)
                    OnMaterialPicked(material);
            };
            button.Unchecked += (_, _) =>
            {
                if (!_updating)
                    UpdateUi();
            };
            Grid.SetColumn(button, i);
            MaterialGrid.Children.Add(button);
            _materialButtons[i] = button;
        }
    }

    private bool Installed => _status.State is EbmState.Enabled or EbmState.Disabled;

    private bool IsPending =>
        _selected != _applied || (Materials.TintApplies(_selected) && _selectedTint != _appliedTint);

    public bool CanToggleDefault => Installed && !_busy;

    public bool IsDefaultApplied => _applied == Material.Default;

    /// <summary>Lays the window out once off-screen, so the first click shows it at its real size.</summary>
    public void Prepare()
    {
        _preparing = true;
        Refresh();
        AppWindow.Move(new PointInt32(-32000, -32000));
        AppWindow.Show(activateWindow: false);
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            AppWindow.Hide();
            _preparing = false;
        });
    }

    public void Toggle()
    {
        if (AppWindow.IsVisible)
        {
            HideFlyout();
            return;
        }
        if (Environment.TickCount64 - _hiddenAt < ReopenGuardMs)
            return;
        ShowFlyout();
    }

    public void ToggleDefaultFromMenu()
    {
        if (!CanToggleDefault)
            return;
        _selected = _applied == Material.Default ? _lastEffect : Material.Default;
        _selectedTint = _appliedTint;
        _ = ApplyAsync();
    }

    public async void ReopenExplorerWindows()
    {
        try
        {
            await ExplorerWindows.ReopenAsync();
        }
        catch (Exception e)
        {
            ShowError(e.Message);
        }
    }

    public void CloseForExit()
    {
        _exiting = true;
        _http.Dispose();
        Close();
    }

    private void ShowFlyout()
    {
        Refresh();
        FitToContent();
        PlaceAboveTray();
        AppWindow.Show();
        Activate();
        SetForegroundWindow(Handle);
        _ = UpdateWindowCountAsync();
    }

    private void HideFlyout()
    {
        if (!AppWindow.IsVisible)
            return;
        AppWindow.Hide();
        _hiddenAt = Environment.TickCount64;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated && !_preparing)
            HideFlyout();
    }

    private double Scale => GetDpiForWindow(Handle) / 96.0;

    private void FitToContent()
    {
        double height = Root.ActualHeight;
        if (height <= 0)
            return;
        // ResizeClient sizes the window as if it still had a title bar, which leaves an empty strip
        // under the content; size the outer window from the frame Windows actually draws instead.
        var outer = AppWindow.Size;
        var client = AppWindow.ClientSize;
        AppWindow.Resize(new SizeInt32(
            (int)Math.Ceiling(WidthDip * Scale) + outer.Width - client.Width,
            (int)Math.Ceiling(height * Scale) + outer.Height - client.Height));
        if (AppWindow.IsVisible && !_preparing)
            PlaceAboveTray();
    }

    /// <summary>Bottom-right corner of the work area on the monitor with the cursor, like system flyouts.</summary>
    private void PlaceAboveTray()
    {
        GetCursorPos(out var cursor);
        var area = DisplayArea.GetFromPoint(new PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Primary).WorkArea;
        int margin = (int)Math.Round(MarginDip * Scale);
        var size = AppWindow.Size;
        AppWindow.Move(new PointInt32(area.X + area.Width - size.Width - margin, area.Y + area.Height - size.Height - margin));
    }

    /// <summary>Reads the installed state and config again; unapplied changes are dropped.</summary>
    private void Refresh()
    {
        _status = _installer.GetStatus();
        if (Installed && !_busy)
        {
            var config = LoadConfig();
            var configured = Materials.FromEffect(config.Effect);
            _lastEffect = configured;
            _applied = _status.State == EbmState.Disabled ? Material.Default : configured;
            var blend = CurrentBlend(config);
            _blendColor = ColorHelper.FromArgb(255, blend.R, blend.G, blend.B);
            _appliedTint = Materials.AlphaToPercent(blend.A);
            _selected = _applied;
            _selectedTint = _appliedTint;
        }
        SetOpen(TransparencyBar, !SystemEffects.IsTransparencyEnabled() || SystemEffects.IsEnergySaverOn());
        UpdateUi();
    }

    // A closed InfoBar still takes a StackPanel slot and its spacing; collapse it as well.
    private static void SetOpen(InfoBar bar, bool open)
    {
        bar.IsOpen = open;
        bar.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUi()
    {
        _updating = true;
        try
        {
            bool installed = Installed;
            EffectPanel.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            Footer.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            InstallPanel.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
            InstallButton.Content = _status.State == EbmState.Broken ? Strings.ReinstallButton : Strings.InstallButton;
            InstallButton.IsEnabled = !_busy;

            for (int i = 0; i < _materialButtons.Length; i++)
            {
                _materialButtons[i].IsChecked = Materials.All[i] == _selected;
                _materialButtons[i].IsEnabled = !_busy;
            }

            bool tintApplies = Materials.TintApplies(_selected);
            TintSlider.Value = _selectedTint;
            TintSlider.IsEnabled = tintApplies && !_busy;
            TintText.Text = tintApplies ? $"{_selectedTint}%" : "—";
            TintHint.Text = _selected switch
            {
                Material.Blur => Strings.TintHintBlur,
                Material.Mica or Material.MicaAlt => Strings.TintHintMica,
                _ => "",
            };
            TintHint.Visibility = TintHint.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            bool pending = IsPending;
            ApplyButton.Content = _busy ? Strings.Applying : Strings.Apply;
            ApplyButton.IsEnabled = pending && !_busy;
            int preset = Array.FindIndex(Materials.BuiltInPresets,
                p => p.Material == _selected && (p.TintPercent is null || p.TintPercent == _selectedTint));
            for (int i = 0; i < _presetItems.Length; i++)
                _presetItems[i].IsChecked = i == preset;
            PresetText.Text = preset >= 0 ? Materials.BuiltInPresets[preset].Name : Strings.PresetCustom;
            PresetButton.IsEnabled = !_busy;

            BlendLayer.Background = installed && tintApplies
                ? new SolidColorBrush(ColorHelper.FromArgb(Materials.PercentToAlpha(_selectedTint), _blendColor.R, _blendColor.G, _blendColor.B))
                : null;

            (StatusText.Text, StatusDot.Fill) = (installed, pending, _applied) switch
            {
                (false, _, _) => (_status.State == EbmState.Broken ? Strings.StatusBroken : Strings.StatusNotInstalled, IdleDot),
                (true, true, _) => (Strings.StatusPending, PendingDot),
                (true, false, Material.Default) => (Strings.StatusDefault, IdleDot),
                _ => (Strings.StatusActive + Strings.StatusWindows(_windowCount), ActiveDot),
            };

            SetBackdrop(installed ? _selected : Material.Acrylic);
        }
        finally
        {
            _updating = false;
        }
    }

    private void SetBackdrop(Material material)
    {
        if (_backdropMaterial == material)
            return;
        _backdropMaterial = material;
        SystemBackdrop = Backdrops.For(material);
        Root.Background = SystemBackdrop is null
            ? (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"]
            : new SolidColorBrush(Colors.Transparent);
    }

    private void OnMaterialPicked(Material material)
    {
        _selected = material;
        if (material != Material.Default)
            _lastEffect = material;
        UpdateUi();
    }

    private void OnTintChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updating)
            return;
        _selectedTint = (int)Math.Round(e.NewValue);
        UpdateUi();
    }

    private void OnPresetButton(object sender, RoutedEventArgs e) =>
        PresetMenu.ShowAt(Root, new FlyoutShowOptions { Placement = FlyoutPlacementMode.LeftEdgeAlignedBottom });

    private void OnPresetPicked(Preset preset)
    {
        _selected = preset.Material;
        if (preset.Material != Material.Default)
            _lastEffect = preset.Material;
        if (preset.TintPercent is { } tint)
            _selectedTint = tint;
        UpdateUi();
    }

    private void OnApply(object sender, RoutedEventArgs e) => _ = ApplyAsync();

    /// <summary>
    /// Material and tint changes go to config.ini and need reopened windows. Switching to or from
    /// Default (un)registers ExplorerBlurMica, which asks for administrator rights and restarts Explorer.
    /// </summary>
    private async Task ApplyAsync()
    {
        if (_busy || !IsPending)
            return;
        _busy = true;
        SetOpen(ErrorBar, false);
        UpdateUi();
        try
        {
            bool wasDefault = _applied == Material.Default;
            bool toDefault = _selected == Material.Default;
            if (!toDefault)
                SaveConfig();

            if (wasDefault != toDefault)
            {
                _status = toDefault ? await _installer.DisableAsync() : await _installer.EnableAsync();
                await ExplorerWindows.RestartExplorerAsync();
            }
            else if (!toDefault)
            {
                await ExplorerWindows.ReopenAsync();
            }

            _applied = _selected;
            _appliedTint = _selectedTint;
        }
        catch (EbmException e) when (e.Error == EbmError.ElevationDeclined)
        {
            // The user said no to UAC; keep their selection so they can try again.
        }
        catch (Exception e)
        {
            ShowError(e.Message);
        }
        finally
        {
            _busy = false;
            UpdateUi();
            _ = UpdateWindowCountAsync();
        }
    }

    private async void OnInstall(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        _busy = true;
        SetOpen(ErrorBar, false);
        InstallProgress.Visibility = Visibility.Visible;
        InstallStatusText.Visibility = Visibility.Visible;
        UpdateUi();

        var progress = new Progress<EbmInstallProgress>(p =>
        {
            InstallStatusText.Text = p.Step switch
            {
                EbmInstallStep.CheckingRelease => Strings.InstallChecking,
                EbmInstallStep.Downloading => Strings.InstallDownloading,
                _ => Strings.InstallRegistering,
            };
            InstallProgress.IsIndeterminate = p.Fraction is null;
            if (p.Fraction is { } fraction)
                InstallProgress.Value = fraction * 100;
        });

        try
        {
            await _installer.InstallLatestAsync(progress);
            InstallStatusText.Text = Strings.InstallRestarting;
            InstallProgress.IsIndeterminate = true;
            await ExplorerWindows.RestartExplorerAsync();
        }
        catch (EbmException ex) when (ex.Error == EbmError.ElevationDeclined)
        {
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            InstallProgress.Visibility = Visibility.Collapsed;
            InstallStatusText.Visibility = Visibility.Collapsed;
            Refresh();
        }
    }

    private void OnOpenColorSettings(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(SystemEffects.TransparencySettingsUri.ToString()) { UseShellExecute = true })?.Dispose();
        HideFlyout();
    }

    private async Task UpdateWindowCountAsync()
    {
        try
        {
            _windowCount = await Task.Run(ExplorerWindows.CountWindows);
            if (!_busy)
                UpdateUi();
        }
        catch (Exception)
        {
            // The count is only decoration for the status line.
        }
    }

    private EbmConfig LoadConfig()
    {
        try
        {
            return EbmConfig.Load(_status.ConfigPath!);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentNullException)
        {
            return new EbmConfig();
        }
    }

    private static BlendColor CurrentBlend(EbmConfig config) =>
        SystemEffects.IsAppsLightTheme() ? config.Light : config.Dark;

    /// <summary>Writes the material and the tint for the theme Windows is in now.</summary>
    private void SaveConfig()
    {
        var config = LoadConfig();
        config.Effect = Materials.ToEffect(_selected);
        if (!Materials.TintApplies(_selected))
        {
            // Keep the stored Acrylic tint for when the user comes back to Acrylic.
            config.Save(_status.ConfigPath!);
            return;
        }
        byte alpha = Materials.PercentToAlpha(_selectedTint);
        if (SystemEffects.IsAppsLightTheme())
            config.Light = config.Light with { A = alpha };
        else
            config.Dark = config.Dark with { A = alpha };
        config.Save(_status.ConfigPath!);
    }

    private void ShowError(string message)
    {
        ErrorBar.Title = Strings.ErrorTitle;
        ErrorBar.Message = message;
        SetOpen(ErrorBar, true);
    }

    private struct Point
    {
        public int X, Y;
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out Point point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hwnd);
}
