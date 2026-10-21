using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace ExplorerBlurMicaTray.Core;

/// <summary>Runs the few steps that need administrator rights in a separate, elevated copy of the app.</summary>
public static class Elevation
{
    private const int ErrorCancelled = 1223;

    public static bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    /// <summary>
    /// Runs an <see cref="ElevatedCommands"/> command: in this process when it is already elevated,
    /// otherwise by starting this executable again through UAC.
    /// </summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> commandArgs, CancellationToken ct = default)
    {
        if (IsElevated)
            return ElevatedCommands.Run(commandArgs);

        string exe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot find the app executable to elevate.");
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = JoinArguments([ElevatedCommands.Flag, .. commandArgs]),
        };

        try
        {
            using var process = Process.Start(psi)!;
            await process.WaitForExitAsync(ct);
            return process.ExitCode;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == ErrorCancelled)
        {
            throw new EbmException(EbmError.ElevationDeclined, "Administrator rights were not granted.", e);
        }
    }

    /// <summary>Quotes arguments the way CommandLineToArgvW splits them back.</summary>
    internal static string JoinArguments(IEnumerable<string> args)
    {
        var sb = new StringBuilder();
        foreach (string arg in args)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            if (arg.Length > 0 && arg.IndexOfAny([' ', '\t', '"']) < 0)
            {
                sb.Append(arg);
                continue;
            }

            sb.Append('"');
            int backslashes = 0;
            foreach (char c in arg)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                sb.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
                backslashes = 0;
                sb.Append(c);
            }
            sb.Append('\\', backslashes * 2);
            sb.Append('"');
        }
        return sb.ToString();
    }
}
