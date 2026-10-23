using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// A small File Explorer window for the Effect preview: tabs, address bar, command bar, folder
/// tree, file list and status bar. Built in code so its text follows the app language. The parts
/// the extra options touch (top bar fill, scroll bar track, divider line) can be switched.
/// </summary>
public sealed partial class ExplorerSketch : Grid
{
    private static readonly SolidColorBrush FolderBrush = new(ColorHelper.FromArgb(255, 240, 193, 76));
    private static readonly SolidColorBrush DocumentBrush = new(ColorHelper.FromArgb(255, 96, 160, 230));
    private static readonly SolidColorBrush ImageBrush = new(ColorHelper.FromArgb(255, 110, 190, 120));

    private readonly Border _tint = new();
    private readonly Border _topFill = new();
    private readonly Border _tab = new();
    private readonly Border _scrollTrack = new() { Width = 10, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly Border _divider = new() { Width = 1 };
    private readonly Border[] _boxes = [new(), new()];
    private readonly Border _selectedNav = new();

    public ExplorerSketch()
    {
        CornerRadius = new CornerRadius(8);
        BorderThickness = new Thickness(1);
        BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(64, 128, 128, 128));
        Children.Add(MicaWallpaper);
        Children.Add(_tint);
        Children.Add(Content());
    }

    /// <summary>Mica shows only the wallpaper right under the window, never the windows behind it.</summary>
    public Image MicaWallpaper { get; } = new()
    {
        Stretch = Stretch.UniformToFill,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    /// <param name="backdrop">What the material draws behind the whole window.</param>
    /// <param name="opaqueTop">The address and command rows keep their own background.</param>
    /// <param name="scrollTrack">The scroll bar keeps its track.</param>
    /// <param name="divider">A line between the folder tree and the file list.</param>
    public void Show(Brush backdrop, bool mica, bool opaqueTop, bool scrollTrack, bool divider)
    {
        _tint.Background = backdrop;
        MicaWallpaper.Visibility = mica ? Visibility.Visible : Visibility.Collapsed;
        var bar = opaqueTop ? Res("SolidBackgroundFillColorTertiaryBrush") : null;
        _topFill.Background = bar;
        _tab.Background = bar ?? Res("ControlFillColorDefaultBrush");
        foreach (var box in _boxes)
            box.Background = Res(opaqueTop ? "ControlFillColorSecondaryBrush" : "ControlFillColorDefaultBrush");
        _selectedNav.Background = Res("ControlFillColorSecondaryBrush");
        _scrollTrack.Background = scrollTrack ? Res("SolidBackgroundFillColorSecondaryBrush") : null;
        _divider.Background = Res("ControlStrongStrokeColorDefaultBrush");
        _divider.Visibility = divider ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    private Grid Content()
    {
        var grid = new Grid();
        foreach (double height in new[] { 32.0, 34, 36, 0, 22 })
            grid.RowDefinitions.Add(new RowDefinition { Height = height > 0 ? new GridLength(height) : new GridLength(1, GridUnitType.Star) });

        SetRow(_topFill, 1);
        SetRowSpan(_topFill, 2);
        grid.Children.Add(_topFill);
        grid.Children.Add(TabRow());
        Place(grid, AddressRow(), 1);
        Place(grid, CommandRow(), 2);
        Place(grid, Body(), 3);
        Place(grid, new TextBlock
        {
            Text = Strings.SketchStatus,
            FontSize = 10,
            Opacity = 0.7,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        }, 4);
        return grid;
    }

    private static void Place(Grid grid, FrameworkElement element, int row)
    {
        SetRow(element, row);
        grid.Children.Add(element);
    }

    private FrameworkElement TabRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _tab.Width = 160;
        _tab.Margin = new Thickness(8, 6, 0, 0);
        _tab.CornerRadius = new CornerRadius(6, 6, 0, 0);
        _tab.Padding = new Thickness(10, 0, 8, 0);
        var tab = new Grid();
        tab.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tab.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tab.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tab.Children.Add(Icon("", 12, FolderBrush));
        var title = Text(Strings.SketchFolder, 11);
        title.Margin = new Thickness(8, 0, 0, 0);
        SetColumn(title, 1);
        tab.Children.Add(title);
        var close = Icon("", 8);
        SetColumn(close, 2);
        tab.Children.Add(close);
        _tab.Child = tab;
        row.Children.Add(_tab);

        var add = Icon("", 10);
        add.Margin = new Thickness(12, 4, 0, 0);
        SetColumn(add, 1);
        add.HorizontalAlignment = HorizontalAlignment.Left;
        row.Children.Add(add);

        var caption = Strip(26, "", "", "");
        caption.Margin = new Thickness(0, 0, 14, 0);
        SetColumn(caption, 2);
        row.Children.Add(caption);
        return row;
    }

    private FrameworkElement AddressRow()
    {
        var row = new Grid { ColumnSpacing = 10, Padding = new Thickness(12, 0, 10, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.Children.Add(Strip(16, "", "", "", ""));

        var path = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        path.Children.Add(Icon("", 11));
        foreach (string part in new[] { Strings.SketchThisPc, Strings.SketchFolder })
        {
            path.Children.Add(Icon("", 8));
            path.Children.Add(Text(part, 11));
        }
        row.Children.Add(Box(_boxes[0], path, 1));

        var search = new Grid();
        search.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        search.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var hint = Text(Strings.SketchSearch, 11);
        hint.Opacity = 0.6;
        search.Children.Add(hint);
        var glass = Icon("", 10);
        SetColumn(glass, 1);
        search.Children.Add(glass);
        row.Children.Add(Box(_boxes[1], search, 2));
        return row;
    }

    private static Border Box(Border box, UIElement content, int column)
    {
        box.Height = 24;
        box.CornerRadius = new CornerRadius(4);
        box.Padding = new Thickness(8, 0, 8, 0);
        box.VerticalAlignment = VerticalAlignment.Center;
        box.Child = content;
        SetColumn(box, column);
        return box;
    }

    private static FrameworkElement CommandRow()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Padding = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(Labeled("", Strings.SketchNew));
        row.Children.Add(Separator());
        row.Children.Add(Strip(16, "", "", "", "", "", ""));
        row.Children.Add(Separator());
        row.Children.Add(Labeled("", Strings.SketchSort));
        row.Children.Add(Labeled("", Strings.SketchView));
        row.Children.Add(Icon("", 11));
        return row;
    }

    private FrameworkElement Body()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nav = new StackPanel { Padding = new Thickness(6, 4, 6, 0) };
        string[] glyphs = ["", "", "", "", "", ""];
        for (int i = 0; i < glyphs.Length; i++)
        {
            var item = Labeled(glyphs[i], Strings.SketchNav[i]);
            item.Height = 22;
            item.Padding = new Thickness(8, 0, 0, 0);
            if (i == 3)
            {
                _selectedNav.CornerRadius = new CornerRadius(4);
                _selectedNav.Child = item;
                nav.Children.Add(_selectedNav);
            }
            else
            {
                nav.Children.Add(item);
            }
        }
        body.Children.Add(nav);

        _divider.Margin = new Thickness(0, 0, 0, 4);
        SetColumn(_divider, 1);
        body.Children.Add(_divider);

        var list = new StackPanel { Padding = new Thickness(12, 4, 14, 0) };
        var header = ListRow(Text(Strings.SketchName, 10), Strings.SketchDate, Strings.SketchType);
        header.Opacity = 0.7;
        list.Children.Add(header);
        foreach (string folder in Strings.SketchFolders)
            list.Children.Add(ListRow(Labeled("", folder, FolderBrush), "20.10.2026 13:55", Strings.SketchFolderType));
        var fileBrushes = new[] { DocumentBrush, DocumentBrush, ImageBrush };
        var fileGlyphs = new[] { "", "", "" };
        for (int i = 0; i < Strings.SketchFiles.Length; i++)
        {
            var (name, type) = Strings.SketchFiles[i];
            list.Children.Add(ListRow(Labeled(fileGlyphs[i], name, fileBrushes[i]), "18.10.2026 09:12", type));
        }
        SetColumn(list, 2);
        body.Children.Add(list);

        SetColumn(_scrollTrack, 2);
        _scrollTrack.Margin = new Thickness(0, 0, 0, 4);
        _scrollTrack.Child = new Rectangle
        {
            Width = 3,
            Height = 54,
            RadiusX = 1.5,
            RadiusY = 1.5,
            Margin = new Thickness(0, 22, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center,
            Fill = Res("TextFillColorTertiaryBrush"),
        };
        body.Children.Add(_scrollTrack);
        return body;
    }

    private static Grid ListRow(FrameworkElement name, string date, string type)
    {
        var row = new Grid { Height = 22 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        row.Children.Add(name);
        var dateText = Text(date, 10);
        dateText.Opacity = 0.7;
        SetColumn(dateText, 1);
        row.Children.Add(dateText);
        var typeText = Text(type, 10);
        typeText.Opacity = 0.7;
        SetColumn(typeText, 2);
        row.Children.Add(typeText);
        return row;
    }

    private static StackPanel Labeled(string glyph, string label, Brush? iconBrush = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(Icon(glyph, 11, iconBrush));
        panel.Children.Add(Text(label, 11));
        return panel;
    }

    private static StackPanel Strip(double spacing, params string[] glyphs)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center };
        foreach (string glyph in glyphs)
            panel.Children.Add(Icon(glyph, 10));
        return panel;
    }

    private static Border Separator() => new()
    {
        Width = 1,
        Height = 16,
        VerticalAlignment = VerticalAlignment.Center,
        Background = Res("DividerStrokeColorDefaultBrush"),
    };

    private static FontIcon Icon(string glyph, double size, Brush? brush = null)
    {
        var icon = new FontIcon { Glyph = glyph, FontSize = size, VerticalAlignment = VerticalAlignment.Center };
        if (brush is not null)
            icon.Foreground = brush;
        return icon;
    }

    private static TextBlock Text(string text, double size) => new()
    {
        Text = text,
        FontSize = size,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };
}
