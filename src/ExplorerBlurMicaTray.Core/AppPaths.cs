namespace ExplorerBlurMicaTray.Core;

/// <summary>Where the app keeps its files.</summary>
public static class AppPaths
{
    public const string AppFolderName = "ExplorerBlurMica-Tray";

    /// <summary>Per-user data: downloads, settings, logs.</summary>
    public static string DataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    public static string DownloadsDir => Path.Combine(DataRoot, "downloads");

    /// <summary>
    /// ExplorerBlurMica is registered machine-wide, so its DLL lives in Program Files where only
    /// administrators can replace it; a per-user folder would let one account inject code into
    /// another account's Explorer.
    /// </summary>
    public static string EbmRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName, "ebm");

    public static string EbmVersionDir(string version) => Path.Combine(EbmRoot, version);
}
