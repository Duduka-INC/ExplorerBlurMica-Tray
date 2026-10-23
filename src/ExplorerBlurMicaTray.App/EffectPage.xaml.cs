using System.Diagnostics;
using ExplorerBlurMicaTray.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

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
        SystemBlendButton.Content = Strings.BlendSystem;
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

        var classicAddress = ExplorerUi.HasClassicAddressBar ? Visibility.Visible : Visibility.Collapsed;
        ClearAddressRow.Visibility = classicAddress;
        ClearAddressLine.Visibility = classicAddress;
        var winUIToolbar = ExplorerUi.HasWinUIToolbar ? Visibility.Visible : Visibility.Collapsed;
        ClearWinUIBgRow.Visibility = winUIToolbar;
        ClearWinUIBgLine.Visibility = winUIToolbar;

        MaterialPicker.SetSize(32, 13);
        MaterialPicker.MaterialPicked += material => _effect?.Edit(d => d with { Material = material });
        Preview.SizeChanged += (_, _) => FitPreview();
        // Explorer follows the Windows theme, and so do the blend color shown and the preview.
        ActualThemeChanged += (_, _) => Refresh();
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

            ColorsSection.Visibility = Materials.TintApplies(draft.Material) ? Visibility.Visible : Visibility.Collapsed;
            ShowBlend(draft.CurrentBlend, editable);

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

    private void ShowBlend(BlendColor color, bool enabled)
    {
        bool light = SystemEffects.IsAppsLightTheme();
        bool system = color.A == 0;
        BlendIcon.Glyph = light ? "" : "";
        BlendTitle.Text = Strings.BlendTitle(light);
        BlendSubtitle.Text = system ? Strings.BlendSystemSubtitle : Strings.BlendCustomSubtitle;
        BlendSwatchFill.Background = new SolidColorBrush(ColorHelper.FromArgb(255, color.R, color.G, color.B));
        BlendPicker.Color = ColorHelper.FromArgb(255, color.R, color.G, color.B);
        int percent = Materials.AlphaToPercent(color.A);
        BlendSlider.Value = percent;
        BlendValue.Text = system ? "" : $"{percent}%";
        BlendSwatch.IsEnabled = enabled;
        BlendSlider.IsEnabled = enabled;
        SystemBlendButton.IsEnabled = enabled && !system;
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

    private void UpdatePreview(EffectSettings draft)
    {
        // Default is the plain Explorer: ExplorerBlurMica is off, so the options don't apply either.
        bool plain = draft.Material == Material.Default;
        bool clearTop = ExplorerUi.HasWinUIToolbar ? draft.ClearWinUIBg : draft.ClearAddress;
        MiniWindow.Show(
            PreviewArt.Backdrop(draft),
            mica: draft.Material is Material.Mica or Material.MicaAlt,
            opaqueTop: plain || !clearTop,
            scrollTrack: plain || !draft.ClearBarBg,
            divider: !plain && draft.ShowLine);
    }

    private void FitPreview()
    {
        // The Mica copy of the wallpaper lines up with the one behind, so the window shows its own slice.
        var mica = MiniWindow.MicaWallpaper;
        mica.Width = Preview.ActualWidth;
        mica.Height = Preview.ActualHeight;
        mica.Margin = new Thickness(-MiniWindow.Margin.Left - 1, -MiniWindow.Margin.Top - 1, 0, 0);
        MiniWindow.Clip = new RectangleGeometry { Rect = new Rect(0, 0, MiniWindow.ActualWidth, MiniWindow.ActualHeight) };
        Preview.Clip = new RectangleGeometry { Rect = new Rect(0, 0, Preview.ActualWidth, Preview.ActualHeight) };
    }

    private async Task LoadWallpaperAsync()
    {
        var bitmap = await PreviewArt.WallpaperAsync();
        WallpaperImage.Source = bitmap;
        MiniWindow.MicaWallpaper.Source = bitmap;
    }

    private void AutomationNames()
    {
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(BlendSwatch, Strings.BlendColorName);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(BlendSlider, Strings.BlendDensity);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearAddressSwitch, Strings.ClearAddressTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearBarBgSwitch, Strings.ClearBarBgTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ClearWinUIBgSwitch, Strings.ClearWinUIBgTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ShowLineSwitch, Strings.ShowLineTitle);
    }

    private void EditBlend(Func<BlendColor, BlendColor> change)
    {
        if (_updating)
            return;
        _effect?.Edit(d => d.WithCurrentBlend(change(d.CurrentBlend)));
    }

    // A color picked over the system tint would stay invisible at zero density, so give it some.
    private void OnBlendColorChanged(ColorPicker sender, ColorChangedEventArgs args) =>
        EditBlend(c => c with
        {
            R = args.NewColor.R,
            G = args.NewColor.G,
            B = args.NewColor.B,
            A = c.A == 0 ? Materials.PercentToAlpha(50) : c.A,
        });

    private void OnBlendAlphaChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        EditBlend(c => c with { A = Materials.PercentToAlpha((int)Math.Round(e.NewValue)) });

    private void OnSystemBlend(object sender, RoutedEventArgs e) =>
        EditBlend(_ => Materials.SystemBlend(SystemEffects.IsAppsLightTheme()));

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
