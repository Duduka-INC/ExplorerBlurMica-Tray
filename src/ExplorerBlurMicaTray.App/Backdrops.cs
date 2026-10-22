using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ExplorerBlurMicaTray.App;

/// <summary>The app's own windows wear the material the user picked for Explorer.</summary>
internal static class Backdrops
{
    /// <summary>The backdrop for <paramref name="material"/>, or null for Default, which draws a plain solid window.</summary>
    public static SystemBackdrop? For(Material material) => material switch
    {
        Material.Mica => new MicaBackdrop { Kind = MicaKind.Base },
        Material.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
        Material.Acrylic => new DesktopAcrylicBackdrop(),
        Material.Blur => new ClearBlurBackdrop(),
        _ => null,
    };
}

/// <summary>Acrylic with almost no tint: the closest system material to ExplorerBlurMica's Blur (Clear).</summary>
internal sealed partial class ClearBlurBackdrop : SystemBackdrop
{
    private DesktopAcrylicController? _controller;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        bool dark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        _controller = new DesktopAcrylicController
        {
            TintColor = dark ? ColorHelper.FromArgb(255, 32, 32, 32) : ColorHelper.FromArgb(255, 243, 243, 243),
            TintOpacity = 0.15f,
            LuminosityOpacity = 0.15f,
        };
        _controller.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));
        _controller.AddSystemBackdropTarget(connectedTarget);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        _controller?.RemoveSystemBackdropTarget(disconnectedTarget);
        _controller?.Dispose();
        _controller = null;
    }
}
