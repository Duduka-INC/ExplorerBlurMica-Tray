using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ExplorerBlurMicaTray.Core;

// COM interfaces for reading open Explorer windows, declared for the COM source generator so they
// work with Native AOT. Methods the app never calls are listed only to keep the vtable order.

[GeneratedComInterface]
[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
internal partial interface IShellWindows
{
    void GetTypeInfoCount(out uint count);
    void GetTypeInfo(uint index, uint lcid, out nint typeInfo);
    void GetIDsOfNames(in Guid riid, nint names, uint count, uint lcid, nint dispIds);
    void Invoke(int dispId, in Guid riid, uint lcid, ushort flags, nint dispParams, nint result, nint excepInfo, nint argErr);
    int GetCount();
    /// <summary>Returns an IDispatch pointer, or zero past the end.</summary>
    nint Item(ComVariant index);
}

[GeneratedComInterface]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal partial interface IComServiceProvider
{
    nint QueryService(in Guid service, in Guid riid);
}

[GeneratedComInterface]
[Guid("000214E2-0000-0000-C000-000000000046")]
internal partial interface IShellBrowser
{
    nint GetWindow();
    void ContextSensitiveHelp(int enterMode);
    void InsertMenusSB(nint menuShared, nint menuWidths);
    void SetMenuSB(nint menuShared, nint oleMenu, nint activeObject);
    void RemoveMenusSB(nint menuShared);
    void SetStatusTextSB(nint statusText);
    void EnableModelessSB(int enable);
    void TranslateAcceleratorSB(nint msg, ushort id);
    void BrowseObject(nint pidl, uint flags);
    nint GetViewStateStream(uint mode);
    nint GetControlWindow(uint id);
    nint SendControlMsg(uint id, uint msg, nint wParam, nint lParam);
    /// <summary>Returns an IShellView pointer.</summary>
    nint QueryActiveShellView();
}

[GeneratedComInterface]
[Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE")]
internal partial interface IFolderView
{
    uint GetCurrentViewMode();
    void SetCurrentViewMode(uint mode);
    nint GetFolder(in Guid riid);
}

[GeneratedComInterface]
[Guid("1AC3D9F0-175C-11D1-95BE-00609797EA4F")]
internal partial interface IPersistFolder2
{
    Guid GetClassID();
    void Initialize(nint pidl);
    /// <summary>Returns a PIDL the caller frees with CoTaskMemFree.</summary>
    nint GetCurFolder();
}

internal static partial class ShellNative
{
    public static readonly Guid ClsidShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    public static readonly Guid SidTopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");

    public const uint ClsctxAll = 0x17;
    public const uint SigdnDesktopAbsoluteParsing = 0x80028000;
    public const uint GaRoot = 2;
    public const uint WmClose = 0x0010;

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    [LibraryImport("shell32.dll")]
    public static partial int SHGetNameFromIDList(nint pidl, uint sigdn, out nint name);

    [LibraryImport("user32.dll")]
    public static partial nint GetAncestor(nint hwnd, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindow(string? className, string? windowName);
}
