using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ExplorerBlurMicaTray.Core;

/// <summary>One open Explorer tab: its top-level window and the folder it shows.</summary>
public sealed record ExplorerWindow(nint WindowHandle, string Location);

/// <summary>
/// Lists and reopens File Explorer windows. ExplorerBlurMica applies its settings when a window
/// opens, so reopening them is how a change takes effect.
/// </summary>
public static class ExplorerWindows
{
    private static readonly StrategyBasedComWrappers Wrappers = new();

    /// <summary>Every open Explorer tab, in the order Windows lists them.</summary>
    /// <param name="onSkipped">Told about each shell window that could not be read.</param>
    public static IReadOnlyList<ExplorerWindow> GetOpen(Action<Exception>? onSkipped = null)
    {
        // Explorer rejects these calls from an MTA thread (RPC_E_CANTCALLOUT_ININPUTSYNCCALL),
        // so they run on a short-lived STA thread whatever thread the caller is on.
        IReadOnlyList<ExplorerWindow> result = [];
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = ReadOnStaThread(onSkipped);
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw failure;
        return result;
    }

    private static List<ExplorerWindow> ReadOnStaThread(Action<Exception>? onSkipped)
    {
        var result = new List<ExplorerWindow>();
        Marshal.ThrowExceptionForHR(ShellNative.CoCreateInstance(
            ShellNative.ClsidShellWindows, 0, ShellNative.ClsctxAll, typeof(IShellWindows).GUID, out nint shellWindowsPtr));
        var shellWindows = Wrap<IShellWindows>(shellWindowsPtr);
        try
        {
            int count = shellWindows.GetCount();
            for (int i = 0; i < count; i++)
            {
                // Skip anything that is not a folder window, or a window that closed meanwhile.
                try
                {
                    if (TryRead(shellWindows, i) is { } window)
                        result.Add(window);
                }
                catch (Exception e) when (e is COMException or InvalidCastException)
                {
                    onSkipped?.Invoke(e);
                }
            }
        }
        finally
        {
            Release(shellWindows);
        }
        return result;
    }

    /// <summary>Number of Explorer windows; tabs of one window count once.</summary>
    public static int CountWindows() => GetOpen().Select(w => w.WindowHandle).Distinct().Count();

    /// <summary>
    /// Closes every Explorer window and opens its folders again, which applies a changed config.
    /// Windows has no way to open a tab from outside, so each tab comes back as its own window.
    /// </summary>
    /// <returns>The number of windows that were closed.</returns>
    public static async Task<int> ReopenAsync(CancellationToken ct = default)
    {
        var tabs = GetOpen();
        var windows = tabs.Select(t => t.WindowHandle).Distinct().ToArray();
        foreach (nint hwnd in windows)
            ShellNative.PostMessageW(hwnd, ShellNative.WmClose, 0, 0);

        await WaitUntil(() => !windows.Any(ShellNative.IsWindow), TimeSpan.FromSeconds(5), ct);
        Open(tabs);
        return windows.Length;
    }

    /// <summary>
    /// Restarts explorer.exe and reopens the folders that were open. ExplorerBlurMica hooks Explorer
    /// when the process starts, so installing, updating or removing it needs a restart, not a reopen.
    /// The taskbar and desktop disappear for a moment.
    /// </summary>
    public static async Task RestartExplorerAsync(CancellationToken ct = default)
    {
        // An Explorer started by an elevated process would run the whole shell elevated.
        if (Elevation.IsElevated)
            throw new InvalidOperationException("Restart Explorer from a non-elevated process.");

        var tabs = GetOpen();
        using var self = Process.GetCurrentProcess();
        var explorers = Process.GetProcessesByName("explorer").Where(p => p.SessionId == self.SessionId).ToList();
        foreach (var process in explorers)
        {
            try
            {
                process.Kill();
            }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
        }
        await WaitUntil(() => explorers.All(p => p.HasExited), TimeSpan.FromSeconds(5), ct);
        foreach (var process in explorers)
            process.Dispose();

        // Windows usually restarts the shell by itself; start it only if it does not.
        if (!await WaitUntil(() => ShellNative.FindWindow("Shell_TrayWnd", null) != 0, TimeSpan.FromSeconds(3), ct))
        {
            Process.Start(new ProcessStartInfo(ExplorerPath) { UseShellExecute = false })?.Dispose();
            await WaitUntil(() => ShellNative.FindWindow("Shell_TrayWnd", null) != 0, TimeSpan.FromSeconds(10), ct);
        }

        Open(tabs);
    }

    private static string ExplorerPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

    private static void Open(IEnumerable<ExplorerWindow> tabs)
    {
        foreach (var tab in tabs)
        {
            var psi = new ProcessStartInfo(ExplorerPath) { UseShellExecute = false };
            psi.ArgumentList.Add(tab.Location);
            Process.Start(psi)?.Dispose();
        }
    }

    private static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
    {
        var waited = Stopwatch.StartNew();
        while (!condition())
        {
            if (waited.Elapsed >= timeout)
                return false;
            await Task.Delay(100, ct);
        }
        return true;
    }

    private static ExplorerWindow? TryRead(IShellWindows shellWindows, int index)
    {
        using var variant = ComVariant.Create(index);
        nint dispatch = shellWindows.Item(variant);
        if (dispatch == 0)
            return null;

        var provider = Wrap<IComServiceProvider>(dispatch);
        IShellBrowser? browser = null;
        IFolderView? view = null;
        IPersistFolder2? folder = null;
        nint pidl = 0;
        try
        {
            browser = Wrap<IShellBrowser>(provider.QueryService(ShellNative.SidTopLevelBrowser, typeof(IShellBrowser).GUID));
            nint root = ShellNative.GetAncestor(browser.GetWindow(), ShellNative.GaRoot);
            view = Wrap<IFolderView>(browser.QueryActiveShellView());
            folder = Wrap<IPersistFolder2>(view.GetFolder(typeof(IPersistFolder2).GUID));
            pidl = folder.GetCurFolder();

            if (ShellNative.SHGetNameFromIDList(pidl, ShellNative.SigdnDesktopAbsoluteParsing, out nint namePtr) < 0)
                return null;
            string? location = Marshal.PtrToStringUni(namePtr);
            Marshal.FreeCoTaskMem(namePtr);
            return string.IsNullOrEmpty(location) || root == 0 ? null : new ExplorerWindow(root, location);
        }
        finally
        {
            if (pidl != 0)
                Marshal.FreeCoTaskMem(pidl);
            Release(folder);
            Release(view);
            Release(browser);
            Release(provider);
        }
    }

    /// <summary>Wraps a COM pointer the callee already AddRef'd and drops that reference.</summary>
    private static T Wrap<T>(nint pointer) where T : class
    {
        if (pointer == 0)
            throw new COMException("Null COM pointer.");
        try
        {
            return (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static void Release(object? comObject)
    {
        if (comObject is ComObject com)
            com.FinalRelease();
    }
}
