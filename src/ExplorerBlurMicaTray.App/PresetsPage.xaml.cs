using ExplorerBlurMicaTray.Core;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// Settings → Presets: the built-in presets and the user's own as tiles with a small preview.
/// Applying a preset applies it to Explorer straight away; the user's presets can be renamed and deleted.
/// </summary>
public sealed partial class PresetsPage : UserControl
{
    private const double ThumbHeight = 104;

    private EffectController? _effect;
    private Preset? _applying;
    private string? _fileError;
    private int _columns;

    public PresetsPage()
    {
        InitializeComponent();
        HeaderText.Text = Strings.NavPresets;
        IntroText.Text = Strings.PresetsIntro;
        SaveButtonText.Text = Strings.PresetSaveCurrent;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(SaveButton, Strings.PresetSaveCurrent);
        EmptyHint.Text = Strings.PresetsEmptyHint;
        ErrorBar.Title = Strings.ErrorTitle;
        ErrorBar.Closed += (_, _) =>
        {
            _fileError = null;
            _effect?.ClearError();
        };
        Tiles.SizeChanged += (_, _) => Arrange();
        ActualThemeChanged += (_, _) => Refresh();
    }

    public void Attach(EffectController effect)
    {
        _effect = effect;
        effect.Changed += () =>
        {
            if (Visibility == Visibility.Visible)
                Refresh();
        };
        _ = LoadWallpaperAsync();
    }

    public void Refresh()
    {
        if (_effect is not { } effect)
            return;
        if (!effect.Busy)
            _applying = null;

        string? error = _fileError ?? effect.Error;
        ErrorBar.Message = error ?? "";
        ErrorBar.IsOpen = error is not null;
        ErrorBar.Visibility = error is not null ? Visibility.Visible : Visibility.Collapsed;
        SaveButton.IsEnabled = effect.Installed;
        EmptyHint.Visibility = effect.Presets.Custom.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Every preset that gives the applied look is marked active. Applying one of them would
        // change nothing, so none of them offers Apply.
        Tiles.Children.Clear();
        foreach (var preset in effect.Presets.All)
            Tiles.Children.Add(Tile(preset, effect.Installed && preset.Matches(effect.Applied), effect));
        _columns = 0;
        Arrange();
    }

    private async Task LoadWallpaperAsync()
    {
        await PreviewArt.WallpaperAsync();
        Refresh();
    }

    private void Arrange()
    {
        int columns = Tiles.ActualWidth >= 700 ? 3 : Tiles.ActualWidth >= 440 ? 2 : 1;
        if (columns == _columns && Tiles.RowDefinitions.Count > 0)
            return;
        _columns = columns;
        Tiles.ColumnDefinitions.Clear();
        Tiles.RowDefinitions.Clear();
        for (int c = 0; c < columns; c++)
            Tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        int rows = (Tiles.Children.Count + columns - 1) / columns;
        for (int r = 0; r < rows; r++)
            Tiles.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < Tiles.Children.Count; i++)
        {
            var tile = (FrameworkElement)Tiles.Children[i];
            Grid.SetColumn(tile, i % columns);
            Grid.SetRow(tile, i / columns);
        }
    }

    private FrameworkElement Tile(Preset preset, bool active, EffectController effect)
    {
        // Built-in presets set only the material and maybe the density, so show them over what's applied.
        var look = preset.ApplyTo(effect.Applied);
        var info = new StackPanel { Padding = new Thickness(14, 12, 12, 12), Spacing = 6 };

        var title = new Grid { ColumnSpacing = 8 };
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        title.Children.Add(new TextBlock
        {
            Text = preset.Name,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (Materials.TintApplies(look.Material))
        {
            var dots = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            dots.Children.Add(Dot(look.Dark, light: false));
            dots.Children.Add(Dot(look.Light, light: true));
            Grid.SetColumn(dots, 1);
            title.Children.Add(dots);
        }
        info.Children.Add(title);

        info.Children.Add(new TextBlock
        {
            Text = Strings.PresetMeta(look.Material, look.CurrentBlend),
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = Res("TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        var actions = new Grid { ColumnSpacing = 4, Margin = new Thickness(0, 4, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actions.Children.Add(Chip(active ? Strings.ChipActive : preset is CustomPreset ? Strings.PresetCustom : Strings.ChipBuiltIn, active));
        if (!active)
        {
            var apply = new Button
            {
                Content = ReferenceEquals(preset, _applying) ? Strings.Applying : Strings.Apply,
                IsEnabled = effect.Installed && !effect.Busy,
            };
            apply.Click += (_, _) => Apply(preset);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(apply, $"{Strings.Apply}: {preset.Name}");
            Grid.SetColumn(apply, 1);
            actions.Children.Add(apply);
        }
        if (preset is CustomPreset custom)
        {
            var more = new Button
            {
                Content = new FontIcon { Glyph = "", FontSize = 14 },
                Padding = new Thickness(8, 6, 8, 6),
                Flyout = CustomMenu(custom),
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(more, $"{Strings.More}: {custom.Name}");
            Grid.SetColumn(more, 2);
            actions.Children.Add(more);
        }
        info.Children.Add(actions);

        var tile = new StackPanel();
        tile.Children.Add(Thumb(look));
        tile.Children.Add(info);
        return new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(active ? 2 : 1),
            BorderBrush = Res(active ? "AccentFillColorDefaultBrush" : "CardStrokeColorDefaultBrush"),
            Background = Res("CardBackgroundFillColorDefaultBrush"),
            Child = tile,
        };
    }

    /// <summary>The Effect preview in small: wallpaper, another window, and a window with the preset's look.</summary>
    private static Grid Thumb(EffectSettings look)
    {
        var wallpaper = PreviewArt.WallpaperAsync() is { IsCompletedSuccessfully: true } task ? task.Result : null;
        var thumb = new Grid { Height = ThumbHeight, Background = new SolidColorBrush(ColorHelper.FromArgb(255, 30, 42, 68)) };
        thumb.Children.Add(new Image { Source = wallpaper, Stretch = Stretch.UniformToFill });
        thumb.Children.Add(new Border
        {
            Width = 70,
            Height = 56,
            Margin = new Thickness(120, 18, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 212, 59)),
        });

        var window = new Grid
        {
            Width = 150,
            Height = 72,
            Margin = new Thickness(16, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(64, 128, 128, 128)),
        };
        // Mica shows only the wallpaper right under the window, so the window gets its own aligned copy.
        var mica = new Image
        {
            Source = wallpaper,
            Stretch = Stretch.UniformToFill,
            Height = ThumbHeight,
            Margin = new Thickness(-17, -17, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = look.Material is Material.Mica or Material.MicaAlt ? Visibility.Visible : Visibility.Collapsed,
        };
        window.Children.Add(mica);
        window.Children.Add(new Border { Background = PreviewArt.Backdrop(look) });
        var bars = new StackPanel { Padding = new Thickness(8), Spacing = 6 };
        foreach (double width in new[] { 50, double.NaN, 90, 75 })
            bars.Children.Add(new Rectangle
            {
                Height = 5,
                Width = width,
                RadiusX = 2.5,
                RadiusY = 2.5,
                HorizontalAlignment = double.IsNaN(width) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
                Fill = Res("TextFillColorPrimaryBrush"),
                Opacity = 0.3,
            });
        window.Children.Add(bars);
        thumb.Children.Add(window);

        thumb.SizeChanged += (_, e) =>
        {
            mica.Width = e.NewSize.Width;
            thumb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        };
        window.SizeChanged += (_, e) => window.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        return thumb;
    }

    private static Ellipse Dot(BlendColor color, bool light)
    {
        // A fully transparent blend color means the system tint.
        var shown = color.A == 0 ? Materials.SystemBlend(light) : color;
        return new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(ColorHelper.FromArgb(255, shown.R, shown.G, shown.B)),
            Stroke = Res("ControlStrokeColorDefaultBrush"),
            StrokeThickness = 1,
        };
    }

    private static Border Chip(string text, bool on) => new()
    {
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(8, 1, 8, 2),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Background = Res(on ? "AccentFillColorDefaultBrush" : "ControlFillColorSecondaryBrush"),
        Child = new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = Res(on ? "TextOnAccentFillColorPrimaryBrush" : "TextFillColorPrimaryBrush"),
        },
    };

    private MenuFlyout CustomMenu(CustomPreset preset)
    {
        var menu = new MenuFlyout();
        var rename = new MenuFlyoutItem { Text = Strings.Rename, Icon = new FontIcon { Glyph = "" } };
        rename.Click += async (_, _) =>
        {
            if (await AskNameAsync(Strings.RenamePresetTitle, preset.Name, preset) is { } name)
                Change(() => _effect!.Presets.Rename(preset, name));
        };
        var delete = new MenuFlyoutItem { Text = Strings.Delete, Icon = new FontIcon { Glyph = "" } };
        delete.Click += async (_, _) =>
        {
            var dialog = Dialog(Strings.DeletePresetTitle(preset.Name), new TextBlock { Text = Strings.DeletePresetBody, TextWrapping = TextWrapping.Wrap });
            dialog.PrimaryButtonText = Strings.Delete;
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                Change(() => _effect!.Presets.Delete(preset));
        };
        menu.Items.Add(rename);
        menu.Items.Add(delete);
        return menu;
    }

    private void Apply(Preset preset)
    {
        if (_effect is not { } effect)
            return;
        _applying = preset;
        effect.Edit(preset.ApplyTo);
        _ = effect.ApplyAsync();
    }

    private async void OnSaveCurrent(object sender, RoutedEventArgs e)
    {
        if (_effect is not { } effect)
            return;
        string suggested = Strings.PresetDefaultName;
        for (int i = 2; effect.Presets.Check(suggested) is not null; i++)
            suggested = $"{Strings.PresetDefaultName} {i}";
        if (await AskNameAsync(Strings.SavePresetTitle, suggested, renaming: null) is { } name)
            Change(() => effect.Presets.Add(name, effect.Draft));
    }

    /// <summary>Runs a change to the preset file and shows why it failed, if it did.</summary>
    private void Change(Action change)
    {
        try
        {
            change();
            _fileError = null;
        }
        catch (IOException ex)
        {
            _fileError = $"{Strings.PresetsSaveError}: {ex.Message}";
        }
        Refresh();
    }

    private async Task<string?> AskNameAsync(string title, string initial, Preset? renaming)
    {
        var box = new TextBox { Text = initial, PlaceholderText = Strings.PresetNamePlaceholder, MaxLength = 60 };
        var problem = new TextBlock
        {
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = Res("SystemFillColorCriticalBrush"),
        };
        var content = new StackPanel { Spacing = 8, MinWidth = 320 };
        content.Children.Add(box);
        content.Children.Add(problem);
        var dialog = Dialog(title, content);
        dialog.PrimaryButtonText = Strings.Save;

        void Check()
        {
            string? why = _effect!.Presets.Check(box.Text, renaming);
            dialog.IsPrimaryButtonEnabled = why is null;
            // An empty box needs no message; the disabled button says enough.
            problem.Text = box.Text.Trim().Length == 0 ? "" : why ?? "";
        }
        box.TextChanged += (_, _) => Check();
        box.Loaded += (_, _) =>
        {
            box.Focus(FocusState.Programmatic);
            box.SelectAll();
        };
        Check();
        return await dialog.ShowAsync() == ContentDialogResult.Primary ? box.Text.Trim() : null;
    }

    private ContentDialog Dialog(string title, UIElement content) => new()
    {
        XamlRoot = XamlRoot,
        Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
        Title = title,
        Content = content,
        CloseButtonText = Strings.Cancel,
        DefaultButton = ContentDialogButton.Primary,
    };

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];
}
