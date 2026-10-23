using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.App;

/// <summary>Everything the user can set for Explorer. <see cref="Material.Default"/> means ExplorerBlurMica is off.</summary>
public sealed record EffectSettings(
    Material Material,
    BlendColor Dark,
    BlendColor Light,
    bool ClearAddress,
    bool ClearBarBg,
    bool ClearWinUIBg,
    bool ShowLine)
{
    public static EffectSettings From(EbmConfig config, bool disabled) => new(
        disabled ? Material.Default : Materials.FromEffect(config.Effect),
        config.Dark, config.Light, config.ClearAddress, config.ClearBarBg, config.ClearWinUIBg, config.ShowLine);

    /// <summary>The blend color for the theme Windows is in now; the flyout's slider edits this one.</summary>
    public BlendColor CurrentBlend => SystemEffects.IsAppsLightTheme() ? Light : Dark;

    public EffectSettings WithCurrentBlend(BlendColor color) =>
        SystemEffects.IsAppsLightTheme() ? this with { Light = color } : this with { Dark = color };

    /// <summary>True when applying this over <paramref name="applied"/> would change what Explorer shows.</summary>
    public bool DiffersFrom(EffectSettings applied) =>
        Material != applied.Material
        || (Material != Material.Default
            && (ClearAddress != applied.ClearAddress || ClearBarBg != applied.ClearBarBg
                || ClearWinUIBg != applied.ClearWinUIBg || ShowLine != applied.ShowLine))
        || (Materials.TintApplies(Material) && (!SameLook(Dark, applied.Dark) || !SameLook(Light, applied.Light)));

    // Any fully transparent blend color means the system tint, whatever its RGB.
    private static bool SameLook(BlendColor a, BlendColor b) => a == b || (a.A == 0 && b.A == 0);

    /// <summary>Default leaves the configured effect alone, so leaving Default brings it back.</summary>
    public void WriteTo(EbmConfig config)
    {
        if (Material != Material.Default)
            config.Effect = Materials.ToEffect(Material);
        config.Dark = Dark;
        config.Light = Light;
        config.ClearAddress = ClearAddress;
        config.ClearBarBg = ClearBarBg;
        config.ClearWinUIBg = ClearWinUIBg;
        config.ShowLine = ShowLine;
    }
}

/// <summary>
/// The state behind the flyout and the settings window: what Explorer shows now (<see cref="Applied"/>),
/// what the user is editing (<see cref="Draft"/>), and the actions that bring the two together.
/// Both windows edit the same draft and redraw on <see cref="Changed"/>.
/// </summary>
public sealed class EffectController : IDisposable
{
    private readonly HttpClient _http = EbmReleaseClient.CreateHttpClient();

    public EffectController()
    {
        Installer = new EbmInstaller(new EbmReleaseClient(_http));
        var defaults = EffectSettings.From(new EbmConfig { Effect = EbmConstants.InstallDefaultEffect }, disabled: false);
        Applied = defaults;
        Draft = defaults;
    }

    /// <summary>Raised on the UI thread whenever anything here changes.</summary>
    public event Action? Changed;

    public EbmInstaller Installer { get; }
    public EbmStatus Status { get; private set; } = new(EbmState.NotInstalled, null, null);
    public EffectSettings Applied { get; private set; }
    public EffectSettings Draft { get; private set; }

    /// <summary>The material to come back to when leaving Default.</summary>
    public Material LastEffect { get; private set; } = Material.Acrylic;

    public bool Busy { get; private set; }
    public int WindowCount { get; private set; }
    public bool TransparencyOff { get; private set; }
    public string? Error { get; private set; }
    public EbmInstallProgress? InstallProgress { get; private set; }

    public bool Installed => Status.State is EbmState.Enabled or EbmState.Disabled;
    public bool IsPending => Installed && Draft.DiffersFrom(Applied);

    /// <summary>Reads the installed state and config again. An unapplied draft is kept.</summary>
    public void Reload()
    {
        Status = Installer.GetStatus();
        if (Installed && !Busy)
        {
            var config = LoadConfig();
            bool keepDraft = IsPending;
            Applied = EffectSettings.From(config, Status.State == EbmState.Disabled);
            if (!keepDraft)
                Draft = Applied;
            LastEffect = Materials.FromEffect(config.Effect);
        }
        TransparencyOff = !SystemEffects.IsTransparencyEnabled() || SystemEffects.IsEnergySaverOn();
        Raise();
    }

    public void Edit(Func<EffectSettings, EffectSettings> change)
    {
        if (Busy)
            return;
        Draft = change(Draft);
        if (Draft.Material != Material.Default)
            LastEffect = Draft.Material;
        Raise();
    }

    public void DiscardDraft()
    {
        Draft = Applied;
        Raise();
    }

    public void ClearError()
    {
        Error = null;
        Raise();
    }

    public Task ToggleDefaultAsync()
    {
        if (!Installed || Busy)
            return Task.CompletedTask;
        Draft = Applied with { Material = Applied.Material == Material.Default ? LastEffect : Material.Default };
        return ApplyAsync();
    }

    /// <summary>
    /// Writes the draft to config.ini and reopens Explorer windows. Switching to or from Default
    /// (un)registers ExplorerBlurMica instead, which asks for administrator rights and restarts Explorer.
    /// </summary>
    public async Task ApplyAsync()
    {
        if (Busy || !IsPending)
            return;
        Busy = true;
        Error = null;
        Raise();
        try
        {
            bool wasDefault = Applied.Material == Material.Default;
            bool toDefault = Draft.Material == Material.Default;
            if (!toDefault)
            {
                var config = LoadConfig();
                Draft.WriteTo(config);
                config.Save(Status.ConfigPath!);
            }

            if (wasDefault != toDefault)
            {
                Status = toDefault ? await Installer.DisableAsync() : await Installer.EnableAsync();
                await ExplorerWindows.RestartExplorerAsync();
            }
            else if (!toDefault)
            {
                await ExplorerWindows.ReopenAsync();
            }
            Applied = Draft;
        }
        catch (EbmException e) when (e.Error == EbmError.ElevationDeclined)
        {
            // The user said no to UAC; keep the draft so they can try again.
        }
        catch (Exception e)
        {
            Error = e.Message;
        }
        finally
        {
            Busy = false;
            Raise();
        }
        await RefreshWindowCountAsync();
    }

    /// <summary>Downloads, verifies and registers ExplorerBlurMica, then restarts Explorer.</summary>
    public async Task InstallAsync()
    {
        if (Busy)
            return;
        Busy = true;
        Error = null;
        InstallProgress = new EbmInstallProgress(EbmInstallStep.CheckingRelease);
        Raise();
        try
        {
            await Installer.InstallLatestAsync(new Progress<EbmInstallProgress>(p =>
            {
                InstallProgress = p;
                Raise();
            }));
            InstallProgress = null;
            await ExplorerWindows.RestartExplorerAsync();
        }
        catch (EbmException e) when (e.Error == EbmError.ElevationDeclined)
        {
        }
        catch (Exception e)
        {
            Error = e.Message;
        }
        finally
        {
            Busy = false;
            InstallProgress = null;
        }
        Reload();
    }

    public async Task ReopenWindowsAsync()
    {
        try
        {
            await ExplorerWindows.ReopenAsync();
        }
        catch (Exception e)
        {
            Error = e.Message;
            Raise();
        }
    }

    public async Task RefreshWindowCountAsync()
    {
        try
        {
            WindowCount = await Task.Run(ExplorerWindows.CountWindows);
            Raise();
        }
        catch (Exception)
        {
            // The count is only decoration for the status line.
        }
    }

    public void Dispose() => _http.Dispose();

    private EbmConfig LoadConfig()
    {
        try
        {
            return EbmConfig.Load(Status.ConfigPath!);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentNullException)
        {
            return new EbmConfig();
        }
    }

    private void Raise() => Changed?.Invoke();
}
