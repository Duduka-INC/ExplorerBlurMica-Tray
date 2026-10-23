using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExplorerBlurMicaTray.App;

/// <summary>One entry of the tray menu; a null <see cref="Text"/> is a separator.</summary>
public sealed record TrayMenuItem(string? Text, Action? Action, bool Checked = false)
{
    public static TrayMenuItem Separator { get; } = new(null, null);
}

/// <summary>
/// The notification-area icon: Shell_NotifyIcon on the flyout's window, a window subclass for its
/// callbacks, and the native popup menu, so it works with Native AOT and without extra packages.
/// </summary>
internal sealed unsafe partial class TrayIcon : IDisposable
{
    private const uint WM_TRAY = 0x8000 + 1; // WM_APP + 1
    private const uint WM_LBUTTONUP = 0x0202, WM_RBUTTONUP = 0x0205;
    private const uint NIM_ADD = 0, NIM_DELETE = 2;
    private const uint NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4;
    private const uint MF_STRING = 0, MF_SEPARATOR = 0x800, MF_CHECKED = 8;
    private const uint TPM_RETURNCMD = 0x100, TPM_RIGHTBUTTON = 2, TPM_BOTTOMALIGN = 0x20, TPM_RIGHTALIGN = 8;

    // uxtheme exports that exist only as ordinals (stable since Windows 10 1903).
    private const int OrdinalAllowDarkModeForWindow = 133;
    private const int OrdinalSetPreferredAppMode = 135;
    private const int OrdinalFlushMenuThemes = 136;
    private const int PreferredAppModeAllowDark = 1;

    private static TrayIcon? s_instance;
    private readonly delegate* unmanaged[Stdcall]<void> _flushMenuThemes;
    private readonly nint _hwnd;
    private readonly nint _hIcon;
    private readonly string _tooltip;
    // Explorer broadcasts this when the taskbar is recreated, e.g. after the app restarts Explorer.
    private readonly uint _taskbarCreated;
    private bool _disposed;

    public Action? LeftClick { get; init; }
    public Func<IEnumerable<TrayMenuItem>>? BuildMenu { get; init; }

    public TrayIcon(nint hwnd, string iconPath, string tooltip)
    {
        _hwnd = hwnd;
        _tooltip = tooltip;
        s_instance = this;
        fixed (char* p = iconPath)
            _hIcon = LoadImageW(0, p, 1 /*IMAGE_ICON*/, GetSystemMetrics(49 /*SM_CXSMICON*/), GetSystemMetrics(50 /*SM_CYSMICON*/), 0x10 /*LR_LOADFROMFILE*/);
        fixed (char* name = "TaskbarCreated")
            _taskbarCreated = RegisterWindowMessageW(name);

        SetWindowSubclass(hwnd, &SubclassProc, 1, 0);
        _flushMenuThemes = EnableDarkMenus(hwnd);
        Add();
    }

    /// <summary>
    /// Native popup menus are light by default; this lets them follow the Windows app theme, as
    /// Notepad++ and other Win32 apps do. Returns FlushMenuThemes, to pick up a theme change.
    /// </summary>
    private static delegate* unmanaged[Stdcall]<void> EnableDarkMenus(nint hwnd)
    {
        if (!NativeLibrary.TryLoad("uxtheme.dll", out nint uxtheme))
            return null;
        var setPreferredAppMode = (delegate* unmanaged[Stdcall]<int, int>)GetProcAddress(uxtheme, OrdinalSetPreferredAppMode);
        var allowDarkModeForWindow = (delegate* unmanaged[Stdcall]<nint, int, int>)GetProcAddress(uxtheme, OrdinalAllowDarkModeForWindow);
        var flushMenuThemes = (delegate* unmanaged[Stdcall]<void>)GetProcAddress(uxtheme, OrdinalFlushMenuThemes);
        if (setPreferredAppMode is not null)
            setPreferredAppMode(PreferredAppModeAllowDark);
        if (allowDarkModeForWindow is not null)
            allowDarkModeForWindow(hwnd, 1);
        return flushMenuThemes;
    }

    private void Add()
    {
        var data = NewData();
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        data.uCallbackMessage = WM_TRAY;
        data.hIcon = _hIcon;
        _tooltip.AsSpan(0, Math.Min(_tooltip.Length, 127)).CopyTo(new Span<char>(data.szTip, 128));
        Shell_NotifyIconW(NIM_ADD, &data);
    }

    private NOTIFYICONDATAW NewData() => new() { cbSize = (uint)sizeof(NOTIFYICONDATAW), hWnd = _hwnd, uID = 1 };

    private void ShowMenu()
    {
        var items = BuildMenu?.Invoke().ToList() ?? [];
        if (items.Count == 0)
            return;

        if (_flushMenuThemes is not null)
            _flushMenuThemes();
        nint menu = CreatePopupMenu();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Text is not { } text)
            {
                AppendMenuW(menu, MF_SEPARATOR, 0, null);
                continue;
            }
            fixed (char* t = text)
                AppendMenuW(menu, MF_STRING | (items[i].Checked ? MF_CHECKED : 0), (nuint)(i + 1), t);
        }

        GetCursorPos(out var pt);
        // Without this the menu would not close when the user clicks elsewhere.
        SetForegroundWindow(_hwnd);
        int cmd = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_RIGHTALIGN, pt.X, pt.Y, 0, _hwnd, 0);
        DestroyMenu(menu);
        if (cmd > 0)
            items[cmd - 1].Action?.Invoke();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint SubclassProc(nint hwnd, uint msg, nint wParam, nint lParam, nuint id, nuint refData)
    {
        if (s_instance is { } tray)
        {
            if (msg == WM_TRAY)
            {
                switch ((uint)lParam)
                {
                    case WM_LBUTTONUP: tray.LeftClick?.Invoke(); break;
                    case WM_RBUTTONUP: tray.ShowMenu(); break;
                }
                return 0;
            }
            if (msg == tray._taskbarCreated && !tray._disposed)
                tray.Add();
        }
        return DefSubclassProc(hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var data = NewData();
        Shell_NotifyIconW(NIM_DELETE, &data);
        RemoveWindowSubclass(_hwnd, &SubclassProc, 1);
        if (_hIcon != 0)
            DestroyIcon(_hIcon);
        s_instance = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        public fixed char szTip[128];
        public uint dwState;
        public uint dwStateMask;
        public fixed char szInfo[256];
        public uint uTimeoutOrVersion;
        public fixed char szInfoTitle[64];
        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    private struct POINT
    {
        public int X, Y;
    }

    [LibraryImport("kernel32.dll")] private static partial nint GetProcAddress(nint module, nint ordinal);
    [LibraryImport("shell32.dll")] private static partial int Shell_NotifyIconW(uint msg, NOTIFYICONDATAW* data);
    [LibraryImport("user32.dll")] private static partial nint LoadImageW(nint hInst, char* name, uint type, int cx, int cy, uint flags);
    [LibraryImport("user32.dll")] private static partial int GetSystemMetrics(int index);
    [LibraryImport("user32.dll")] private static partial uint RegisterWindowMessageW(char* name);
    [LibraryImport("user32.dll")] private static partial int DestroyIcon(nint hIcon);
    [LibraryImport("user32.dll")] private static partial nint CreatePopupMenu();
    [LibraryImport("user32.dll")] private static partial int AppendMenuW(nint menu, uint flags, nuint id, char* text);
    [LibraryImport("user32.dll")] private static partial int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);
    [LibraryImport("user32.dll")] private static partial int DestroyMenu(nint menu);
    [LibraryImport("user32.dll")] private static partial int GetCursorPos(out POINT pt);
    [LibraryImport("user32.dll")] private static partial int SetForegroundWindow(nint hwnd);
    [LibraryImport("comctl32.dll")] private static partial int SetWindowSubclass(nint hwnd, delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nuint, nuint, nint> proc, nuint id, nuint refData);
    [LibraryImport("comctl32.dll")] private static partial int RemoveWindowSubclass(nint hwnd, delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nuint, nuint, nint> proc, nuint id);
    [LibraryImport("comctl32.dll")] private static partial nint DefSubclassProc(nint hwnd, uint msg, nint wParam, nint lParam);
}
