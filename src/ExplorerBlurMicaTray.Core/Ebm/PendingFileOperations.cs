using Microsoft.Win32;

namespace ExplorerBlurMicaTray.Core;

/// <summary>
/// File operations Windows carries out at the next restart. Uninstall leaves the DLL Explorer still
/// holds there for deletion; reinstalling before a restart must take it off the list, or Windows
/// deletes the freshly installed DLL on boot.
/// </summary>
internal static class PendingFileOperations
{
    private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager";
    private const string ValueName = "PendingFileRenameOperations";

    /// <summary>Cancels pending deletions of anything under <paramref name="directory"/>. Needs an elevated process.</summary>
    /// <returns>How many deletions were cancelled.</returns>
    public static int CancelDeletionsUnder(string directory)
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true);
        if (key?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is not string[] entries)
            return 0;

        string[] kept = RemoveDeletionsUnder(entries, directory, out int removed);
        if (removed == 0)
            return 0;
        if (kept.Length == 0)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        else
            key.SetValue(ValueName, kept, RegistryValueKind.MultiString);
        return removed;
    }

    /// <summary>
    /// Entries come in pairs: a source path, then a destination; an empty destination means delete.
    /// Renames and entries for other folders are kept as they are.
    /// </summary>
    internal static string[] RemoveDeletionsUnder(string[] entries, string directory, out int removed)
    {
        string root = Path.GetFullPath(directory).TrimEnd('\\');
        var kept = new List<string>(entries.Length);
        removed = 0;
        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            if (entries[i + 1].Length == 0 && ToPath(entries[i]) is { } path && IsUnder(path, root))
            {
                removed++;
                continue;
            }
            kept.Add(entries[i]);
            kept.Add(entries[i + 1]);
        }
        if (entries.Length % 2 == 1)
            kept.Add(entries[^1]);
        return [.. kept];
    }

    // Sources look like "\??\C:\dir\file"; recent Windows versions put flags such as "*1" in front.
    private static string? ToPath(string source)
    {
        int nt = source.IndexOf(@"\??\", StringComparison.Ordinal);
        return nt < 0 ? null : source[(nt + 4)..].TrimEnd('\\');
    }

    private static bool IsUnder(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
}
