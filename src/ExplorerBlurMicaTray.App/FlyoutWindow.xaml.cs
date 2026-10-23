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

    private readonly EffectController _effect;
    private readonly RadioMenuFlyoutItem[] _presetItems;
    private Material? _backdropMaterial;
    private bool _updating;
    private bool _preparing;
    private bool _exiting;
    private long _hiddenAt;

    public FlyoutWindow(EffectController effect)
    {
        InitializeComponent();
        _effect = effect;
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
        ErrorBar.Closed += (_, _) => _effect.ClearError();

        TitleText.Text = Strings.FlyoutTitle;
        ToolTipService.SetToolTip(SettingsButton, Strings.OpenSettings);
        TransparencyBar.Title = Strings.TransparencyOffTitle;
        TransparencyBar.Message = Strings.TransparencyOffMessage;
        ColorSettingsButton.Content = Strings.OpenWindowsSettings;
        InstallTitleText.Text = Strings.InstallTitle;
        InstallBodyText.Text = Strings.InstallBody;
        InstallNoteText.Text = Strings.InstallNote;
        MaterialPicker.SetSize(28, 12);
        MaterialPicker.MaterialPicked += material => _effect.Edit(d => d with { Material = material });
        _presetItems = Materials.BuiltInPresets.Select(preset =>
        {
            var item = new RadioMenuFlyoutItem { Text = preset.Name, GroupName = "presets" };
            item.Click += (_, _) => _effect.Edit(preset.ApplyTo);
            PresetMenu.Items.Add(item);
            return item;
        }).ToArray();

        _effect.Changed += () =>
        {
            if (AppWindow.IsVisible || _preparing)
                UpdateUi();
        };
    }

    public nint Handle { get; }

    /// <summary>Raised by the gear button.</summary>
    public event Action? SettingsRequested;

    /// <summary>Lays the window out once off-screen, so the first click shows it at its real size.</summary>
    public void Prepare()
    {
        _preparing = true;
        _effect.Reload();
        UpdateUi();
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

    public void CloseForExit()
    {
        _exiting = true;
        Close();
    }

    private void ShowFlyout()
    {
        _effect.Reload();
        UpdateUi();
        FitToContent();
        PlaceAboveTray();
        AppWindow.Show();
        Activate();
        SetForegroundWindow(Handle);
        _ = _effect.RefreshWindowCountAsync();
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

    // A closed InfoBar still takes a StackPanel slot and its spacing; collapse it as well.
    private static void SetOpen(InfoBar bar, bool open)
    {
        bar.IsOpen = open;
        bar.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUi()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            var effect = _effect;
            var draft = effect.Draft;
            bool installed = effect.Installed;
            bool busy = effect.Busy;

            SetOpen(TransparencyBar, effect.TransparencyOff);
            ErrorBar.Title = Strings.ErrorTitle;
            ErrorBar.Message = effect.Error ?? "";
            SetOpen(ErrorBar, effect.Error is not null);

            EffectPanel.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            Footer.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            InstallPanel.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
            InstallButton.Content = effect.Status.State == EbmState.Broken ? Strings.ReinstallButton : Strings.InstallButton;
            InstallButton.IsEnabled = !busy;
            ShowInstallProgress(effect.InstallProgress);

            MaterialPicker.Show(draft.Material, !busy);

            bool tintApplies = Materials.TintApplies(draft.Material);
            int tint = Materials.AlphaToPercent(draft.CurrentBlend.A);
            TintSlider.Value = tint;
            TintSlider.IsEnabled = tintApplies && !busy;
            // At zero ExplorerBlurMica keeps the system acrylic tint.
            TintText.Text = !tintApplies ? "—" : tint == 0 ? Strings.BlendSystem : $"{tint}%";
            TintHint.Text = Materials.TintHint(draft.Material);
            TintHint.Visibility = TintHint.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            bool pending = effect.IsPending;
            ApplyButton.Content = busy ? Strings.Applying : Strings.Apply;
            ApplyButton.IsEnabled = pending && !busy;
            int preset = Materials.MatchPreset(draft);
            for (int i = 0; i < _presetItems.Length; i++)
                _presetItems[i].IsChecked = i == preset;
            PresetText.Text = preset >= 0 ? Materials.BuiltInPresets[preset].Name : Strings.PresetCustom;
            PresetButton.IsEnabled = !busy;

            var blend = draft.CurrentBlend;
            BlendLayer.Background = installed && tintApplies
                ? new SolidColorBrush(ColorHelper.FromArgb(blend.A, blend.R, blend.G, blend.B))
                : null;

            (StatusText.Text, StatusDot.Fill) = (installed, pending, effect.Applied.Material) switch
            {
                (false, _, _) => (effect.Status.State == EbmState.Broken ? Strings.StatusBroken : Strings.StatusNotInstalled, IdleDot),
                (true, true, _) => (Strings.StatusPending, PendingDot),
                (true, false, Material.Default) => (Strings.StatusDefault, IdleDot),
                _ => (Strings.StatusActive + Strings.StatusWindows(effect.WindowCount), ActiveDot),
            };

            SetBackdrop(installed ? draft.Material : Material.Acrylic);
        }
        finally
        {
            _updating = false;
        }
    }

    private void ShowInstallProgress(EbmInstallProgress? progress)
    {
        var visibility = progress is null ? Visibility.Collapsed : Visibility.Visible;
        InstallProgress.Visibility = visibility;
        InstallStatusText.Visibility = visibility;
        if (progress is null)
            return;
        InstallStatusText.Text = progress.Step switch
        {
            EbmInstallStep.CheckingRelease => Strings.InstallChecking,
            EbmInstallStep.Downloading => Strings.InstallDownloading,
            _ => Strings.InstallRegistering,
        };
        InstallProgress.IsIndeterminate = progress.Fraction is null;
        if (progress.Fraction is { } fraction)
            InstallProgress.Value = fraction * 100;
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

    private void OnTintChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updating)
            return;
        byte alpha = Materials.PercentToAlpha((int)Math.Round(e.NewValue));
        _effect.Edit(d => d.WithCurrentBlend(d.CurrentBlend with { A = alpha }));
    }

    private void OnPresetButton(object sender, RoutedEventArgs e) =>
        PresetMenu.ShowAt(Root, new FlyoutShowOptions { Placement = FlyoutPlacementMode.LeftEdgeAlignedBottom });

    private void OnApply(object sender, RoutedEventArgs e) => _ = _effect.ApplyAsync();

    private void OnInstall(object sender, RoutedEventArgs e) => _ = _effect.InstallAsync();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        HideFlyout();
        SettingsRequested?.Invoke();
    }

    private void OnOpenColorSettings(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(SystemEffects.TransparencySettingsUri.ToString()) { UseShellExecute = true })?.Dispose();
        HideFlyout();
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
