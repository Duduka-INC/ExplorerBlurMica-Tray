using System.Globalization;
using ExplorerBlurMicaTray.Core;

// A command-line front end for the core, used to try it out without the tray app.
if (ElevatedCommands.TryRun(args, out int elevatedExit))
    return elevatedExit;

using var http = EbmReleaseClient.CreateHttpClient();
var releases = new EbmReleaseClient(http);
var installer = new EbmInstaller(releases);

try
{
    return args.FirstOrDefault() switch
    {
        null or "status" => Status(),
        "latest" => await Latest(),
        "install" => await Install(),
        "uninstall" => await Uninstall(),
        "config" => Config(args[1..]),
        "windows" => Windows(),
        "reopen" => await Reopen(),
        _ => Help(),
    };
}
catch (EbmException e)
{
    Console.Error.WriteLine($"error ({e.Error}): {e.Message}");
    return 1;
}

int Status()
{
    var status = installer.GetStatus();
    Console.WriteLine($"ExplorerBlurMica:   {status.State}{(status.Version is null ? "" : $" {status.Version}")}");
    if (status.DllPath is not null)
        Console.WriteLine($"DLL:                {status.DllPath}");
    Console.WriteLine($"Transparency:       {(SystemEffects.IsTransparencyEnabled() ? "on" : "off")}");
    Console.WriteLine($"Energy saver:       {(SystemEffects.IsEnergySaverOn() ? "on" : "off")}");
    Console.WriteLine($"Explorer windows:   {ExplorerWindows.CountWindows()}");
    return 0;
}

async Task<int> Latest()
{
    var release = await releases.GetLatestAsync();
    Console.WriteLine($"{release.Version}  {release.Size} bytes  sha256 {release.Sha256}");
    Console.WriteLine(release.DownloadUrl);
    return 0;
}

async Task<int> Install()
{
    var progress = new Progress<EbmInstallProgress>(p =>
        Console.WriteLine(p.Fraction is { } f ? $"{p.Step} {f:P0}" : p.Step.ToString()));
    var status = await installer.InstallLatestAsync(progress);
    Console.WriteLine($"Installed {status.Version} at {status.DllPath}");
    return 0;
}

async Task<int> Uninstall()
{
    var status = await installer.UninstallAsync();
    Console.WriteLine($"ExplorerBlurMica: {status.State}");
    return 0;
}

int Config(string[] rest)
{
    string? path = installer.GetStatus().ConfigPath;
    if (path is null || !File.Exists(path))
    {
        Console.Error.WriteLine("ExplorerBlurMica is not installed.");
        return 1;
    }

    var config = EbmConfig.Load(path);
    if (rest is ["set", var key, var value])
    {
        if (!TrySet(config, key, value))
        {
            Console.Error.WriteLine($"Cannot set '{key}' to '{value}'.");
            return 2;
        }
        config.Save(path);
    }
    else if (rest.Length > 0)
    {
        return Help();
    }

    Console.WriteLine(path);
    Console.WriteLine($"effect={config.Effect} clearAddress={config.ClearAddress} clearBarBg={config.ClearBarBg} clearWinUIBg={config.ClearWinUIBg} showLine={config.ShowLine}");
    Console.WriteLine($"light={config.Light} dark={config.Dark}");
    return 0;
}

static bool TrySet(EbmConfig config, string key, string value)
{
    switch (key)
    {
        case "effect" when Enum.TryParse(value, ignoreCase: true, out EbmEffect effect) && Enum.IsDefined(effect):
            config.Effect = effect;
            return true;
        case "clearAddress" when bool.TryParse(value, out bool b):
            config.ClearAddress = b;
            return true;
        case "clearBarBg" when bool.TryParse(value, out bool b):
            config.ClearBarBg = b;
            return true;
        case "clearWinUIBg" when bool.TryParse(value, out bool b):
            config.ClearWinUIBg = b;
            return true;
        case "showLine" when bool.TryParse(value, out bool b):
            config.ShowLine = b;
            return true;
        case "darkAlpha" when byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte a):
            config.Dark = config.Dark with { A = a };
            return true;
        case "lightAlpha" when byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte a):
            config.Light = config.Light with { A = a };
            return true;
        default:
            return false;
    }
}

int Windows()
{
    foreach (var window in ExplorerWindows.GetOpen(e => Console.Error.WriteLine($"skipped: {e.GetType().Name}: {e.Message}")))
        Console.WriteLine($"0x{window.WindowHandle:X8}  {window.Location}");
    return 0;
}

async Task<int> Reopen()
{
    int closed = await ExplorerWindows.ReopenAsync();
    Console.WriteLine($"Reopened {closed} window(s).");
    return 0;
}

static int Help()
{
    Console.WriteLine("""
        ebmt status                      ExplorerBlurMica state, transparency, open windows
        ebmt latest                      latest official release on GitHub
        ebmt install                     download, verify and register (asks for admin rights)
        ebmt uninstall                   unregister (asks for admin rights)
        ebmt config                      show config.ini
        ebmt config set <key> <value>    effect | clearAddress | clearBarBg | clearWinUIBg | showLine | darkAlpha | lightAlpha
        ebmt windows                     list open Explorer tabs
        ebmt reopen                      close and reopen Explorer windows
        """);
    return 2;
}
