using System.Diagnostics;
using ExplorerBlurMicaTray.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Windows.Foundation;
using Windows.UI;

namespace ExplorerBlurMicaTray.App;

/// <summary>Settings → Effect: material, blend colors for both themes and the extra options, with a preview.</summary>
public sealed partial class EffectPage : UserControl
{
    private EffectController? _effect;
    private bool _updating;

    public EffectPage()
    {
        InitializeComponent();

        HeaderText.Text = Strings.NavEffect;
        PreviewLabel.Text = Strings.Preview;
        MaterialTitle.Text = Strings.MaterialTitle;
        MaterialSubtitle.Text = Strings.MaterialSubtitle;
        ColorsHeader.Text = Strings.ColorsHeader;
        DarkTitle.Text = Strings.DarkTheme;
        LightTitle.Text = Strings.LightTheme;
        DarkSubtitle.Text = Strings.BlendSubtitle;
        LightSubtitle.Text = Strings.BlendSubtitle;
        AdvancedTitle.Text = Strings.AdvancedTitle;
        AdvancedSubtitle.Text = Strings.AdvancedSubtitle;
        ClearAddressTitle.Text = Strings.ClearAddressTitle;
        ClearAddressDesc.Text = Strings.ClearAddressDesc;
        ClearBarBgTitle.Text = Strings.ClearBarBgTitle;
        ClearBarBgDesc.Text = Strings.ClearBarBgDesc;
        ClearWinUIBgTitle.Text = Strings.ClearWinUIBgTitle;
        ClearWinUIBgDesc.Text = Strings.ClearWinUIBgDesc;
        ShowLineTitle.Text = Strings.ShowLineTitle;
        ShowLineDesc.Text = Strings.ShowLineDesc;
        TransparencyBar.Title = Strings.TransparencyOffTitle;
        TransparencyBar.Message = Strings.TransparencyOffMessage;
        ColorSettingsButton.Content = Strings.OpenWindowsSettings;
        NotInstalledBar.Title = Strings.InstallTitle;
        NotInstalledBar.Message = Strings.InstallBody;
        DiscardButton.Content = Strings.Discard;
        ErrorBar.Title = Strings.ErrorTitle;
        ErrorBar.Closed += (_, _) => _effect?.ClearError();
        AutomationNames();

        MaterialPicker.SetSize(32, 13);
        MaterialPicker.MaterialPicked += material => _effect?.Edit(d => d with { Material = material });
        Preview.SizeChanged += (_, _) => FitPreview();
        _ = LoadWallpaperAsync();
    }

    public void Attach(EffectController effect)
    {
        _effect = effect;
        effect.Changed += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        if (_effect is not { } effect || _updating)
            return;
        _updating = true;
        try
        {
            var draft = effect.Draft;
            bool installed = effect.Installed;
            bool busy = effect.Busy;
            bool editable = installed && !busy;

            SetOpen(NotInstalledBar, !installed);
            InstallButton.Content = effect.InstallProgress is { } progress
                ? progress.Step == EbmInstallStep.Downloading ? Strings.InstallDownloading : Strings.InstallRegistering
                : effect.Status.State == EbmState.Broken ? Strings.ReinstallButton : Strings.InstallButton;
            InstallButton.IsEnabled = !busy;
            SetOpen(TransparencyBar, effect.TransparencyOff);
            ErrorBar.Message = effect.Error ?? "";
            SetOpen(ErrorBar, effect.Error is not null);

            bool pending = effect.IsPending;
            SetOpen(PendingBar, pending || busy);
            bool needsRestart = (draft.Material == Material.Default) != (effect.Applied.Material == Material.Default);
            PendingBar.Title = Strings.PendingTitle;
            PendingBar.Message = needsRestart
                ? Strings.PendingRestart(enable: draft.Material != Material.Default)
                : Strings.PendingReopen(effect.WindowCount);
            ApplyButton.Content = busy ? Strings.Applying : Strings.Apply;
            ApplyButton.IsEnabled = pending && !busy;
            DiscardButton.IsEnabled = pending && !busy;

            MaterialPicker.Show(draft.Material, editable);
            MaterialDescription.Text = Strings.MaterialDescription(draft.Material);

            bool tint = editable && Materials.TintApplies(draft.Material);
            ShowBlend(draft.Dark, DarkSwatch, DarkSwatchFill, DarkPicker, DarkSlider, DarkValue, tint);
            ShowBlend(draft.Light, LightSwatch, LightSwatchFill, LightPicker, LightSlider, LightValue, tint);
            TintHint.Text = Materials.TintHint(draft.Material);
            TintHint.Visibility = TintHint.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            bool extras = editable && draft.Material != Material.Default;
            ShowSwitch(ClearAddressSwitch, draft.ClearAddress, extras);
            ShowSwitch(ClearBarBgSwitch, draft.ClearBarBg, extras);
            ShowSwitch(ClearWinUIBgSwitch, draft.ClearWinUIBg, extras);
            ShowSwitch(ShowLineSwitch, draft.ShowLine, extras);

            UpdatePreview(draft);
        }
        finally
        {
            _updating = false;
        }
    }

    private static void ShowBlend(BlendColor color, Button swatch, Border fill, ColorPicker picker, Slider slider, TextBlock value, bool enabled)
    {
        fill.Background = new SolidColorBrush(ColorHelper.FromArgb(255, color.R, color.G, color.B));
        picker.Color = ColorHelper.FromArgb(255, color.R, color.G, color.B);
        int percent = Materials.AlphaToPercent(color.A);
        slider.Value = percent;
        value.Text = enabled ? $"{percent}%" : "—";
        swatch.IsEnabled = enabled;
        slider.IsEnabled = enabled;
    }

    private static void ShowSwitch(ToggleSwitch toggle, bool on, bool enabled)
    {
        toggle.IsOn = on;
        toggle.IsEnabled = enabled;
    }

    // A closed InfoBar still takes a StackPanel slot and its spacing; collapse it as well.
    private static void SetOpen(InfoBar bar, bool open)
    {
        bar.IsOpen = open;
        bar.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// In-app acrylic blurs whatever is behind it in this window, which is close to what each
    /// material does to Explorer: Acrylic and Blur show the window behind, Mica only the wallpaper.
    /// </summary>
    private void UpdatePreview(EffectSettings draft)
    {
        Color solid = SystemEffects.IsAppsLightTheme() ? ColorHelper.FromArgb(255, 243, 243, 243) : ColorHelper.FromArgb(255, 32, 32, 32);
        MicaWallpaper.Visibility = draft.Material is Material.Mica or Material.MicaAlt ? Visibility.Visible : Visibility.Collapsed;
        MiniTint.Background = draft.Material switch
        {
            Material.Default => new SolidColorBrush(solid),
            Material.Acrylic => Acrylic(draft.CurrentBlend, solid),
            Material.Blur => new AcrylicBrush { TintColor = solid, TintOpacity = 0, TintLuminosityOpacity = 0, FallbackColor = solid },
            Material.MicaAlt => new AcrylicBrush { TintColor = solid, TintOpacity = 0.45, TintLuminosityOpacity = 0.3, FallbackColor = solid },
            _ => new AcrylicBrush { TintColor = solid, TintOpacity = 0.75, TintLuminosityOpacity = 0.6, FallbackColor = solid },
        };
    }

    // ExplorerBlurMica falls back to the system acrylic when the blend color is fully transparent.
    private static AcrylicBrush Acrylic(BlendColor blend, Color solid) => blend.A == 0
        ? new AcrylicBrush { TintColor = solid, TintOpacity = 0.6, FallbackColor = solid }
        : new AcrylicBrush
        {
            TintColor = ColorHelper.FromArgb(255, blend.R, blend.G, blend.B),
            TintOpacity = blend.A / 255.0,
            TintLuminosityOpacity = 0,
            FallbackColor = solid,
        };

    private void FitPreview()
    {
        // The Mica copy of the wallpaper lines up with the one behind, so the window shows its own slice.
        MicaWallpaper.Width = Preview.ActualWidth;
        MicaWallpaper.Height = Preview.ActualHeight;
        MiniWindow.Clip = new RectangleGeometry { Rect = new Rect(0, 0, MiniWindow.ActualWidth, MiniWindow.ActualHeight) };
        Preview.Clip = new RectangleGeometry { Rect = new Rect(0, 0, Preview.ActualWidth, Preview.ActualHeight) };
    }

    private async Task LoadWallpaperAsync()
    {
        if (FindWallpaper() is not { } path)
            return;
        try
        {
            var bitmap = new BitmapImage { DecodePixelWidth = 1280 };
            using var file = File.OpenRead(path);
            using var stream = file.AsRandomAccessStream();
            await bitmap.SetSourceAsync(stream);
            WallpaperImage.Source = bitmap;
            MicaWallpaper.Source = bitmap;
        }
        catch (Exception)
        {
            // Without a readable wallpaper the preview keeps its plain background.
        }
    }

    /// <summary>Windows keeps a copy of the current wallpaper, slideshows and Spotlight included.</summary>
    private static string? FindWallpaper()
    {
        string transcoded = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
        if (File.Exists(transcoded))
            return transcoded;
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        return key?.GetValue("WallPaper") is string { Length: > 0 } path && File.Exists(path) ? path : null;
    }

    private void AutomationNames()
    {
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(DarkSwatch, Strings.DarkTheme);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(LightSwatch, Strings.LightTheme);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(DarkSlider, Strings.DarkTheme);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(LightSlider, Strings.LightTheme);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearAddressSwitch, Strings.ClearAddressTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearBarBgSwitch, Strings.ClearBarBgTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearWinUIBgSwitch, Strings.ClearWinUIBgTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ShowLineSwitch, Strings.ShowLineTitle);
    }

    private void EditBlend(bool dark, Func<BlendColor, BlendColor> change)
    {
        if (_updating)
            return;
        _effect?.Edit(d => dark ? d with { Dark = change(d.Dark) } : d with { Light = change(d.Light) });
    }

    private void OnDarkColorChanged(ColorPicker sender, ColorChangedEventArgs args) =>
        EditBlend(dark: true, c => c with { R = args.NewColor.R, G = args.NewColor.G, B = args.NewColor.B });

    private void OnLightColorChanged(ColorPicker sender, ColorChangedEventArgs args) =>
        EditBlend(dark: false, c => c with { R = args.NewColor.R, G = args.NewColor.G, B = args.NewColor.B });

    private void OnDarkAlphaChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        EditBlend(dark: true, c => c with { A = Materials.PercentToAlpha((int)Math.Round(e.NewValue)) });

    private void OnLightAlphaChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        EditBlend(dark: false, c => c with { A = Materials.PercentToAlpha((int)Math.Round(e.NewValue)) });

    private void OnAdvancedToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _effect is null)
            return;
        _effect.Edit(d => d with
        {
            ClearAddress = ClearAddressSwitch.IsOn,
            ClearBarBg = ClearBarBgSwitch.IsOn,
            ClearWinUIBg = ClearWinUIBgSwitch.IsOn,
            ShowLine = ShowLineSwitch.IsOn,
        });
    }

    private void OnApply(object sender, RoutedEventArgs e) => _ = _effect?.ApplyAsync();

    private void OnDiscard(object sender, RoutedEventArgs e) => _effect?.DiscardDraft();

    private void OnInstall(object sender, RoutedEventArgs e) => _ = _effect?.InstallAsync();

    private void OnOpenColorSettings(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(SystemEffects.TransparencySettingsUri.ToString()) { UseShellExecute = true })?.Dispose();
}
