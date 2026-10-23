using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ExplorerBlurMicaTray.App;

/// <summary>The row of material buttons shared by the flyout and the settings window.</summary>
public sealed partial class MaterialSelector : Grid
{
    private readonly ToggleButton[] _buttons = new ToggleButton[Materials.All.Length];
    private bool _updating;
    private bool _enabled = true;

    public MaterialSelector()
    {
        ColumnSpacing = 2;
        for (int i = 0; i < Materials.All.Length; i++)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var material = Materials.All[i];
            var button = new ToggleButton
            {
                Content = Materials.Label(material),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0),
            };
            // Checked/Unchecked, not Click, so keyboard and UI Automation toggles count too.
            button.Checked += (_, _) =>
            {
                if (!_updating)
                    MaterialPicked?.Invoke(material);
            };
            button.Unchecked += (_, _) =>
            {
                if (!_updating)
                    Show(Selected, _enabled);
            };
            SetColumn(button, i);
            Children.Add(button);
            _buttons[i] = button;
        }
    }

    public event Action<Material>? MaterialPicked;

    public Material Selected { get; private set; }

    /// <summary>Button height and font size; the flyout uses a compact row.</summary>
    public void SetSize(double height, double fontSize)
    {
        foreach (var button in _buttons)
        {
            button.Height = height;
            button.FontSize = fontSize;
        }
    }

    public void Show(Material selected, bool enabled)
    {
        Selected = selected;
        _enabled = enabled;
        _updating = true;
        try
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].IsChecked = Materials.All[i] == selected;
                _buttons[i].IsEnabled = enabled;
            }
        }
        finally
        {
            _updating = false;
        }
    }
}
