using ExplorerBlurMicaTray.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ExplorerBlurMicaTray.App;

public static class Program
{
    /// <summary>Also checked by the installer (AppMutex) so it can ask to close the app first.</summary>
    public const string MutexName = "ExplorerBlurMicaTray.Running";

    /// <summary>Passed by the app when it restarts itself: wait for the old instance instead of quitting.</summary>
    public const string RestartArgument = "--restart";

    [STAThread]
    private static int Main(string[] args)
    {
        // The app relaunches itself through UAC for the steps that need administrator rights.
        if (ElevatedCommands.TryRun(args, out int exitCode))
            return exitCode;

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool firstInstance);
        if (!firstInstance && !(args.Contains(RestartArgument) && WaitForPrevious(mutex)))
            return 0;

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(initParams =>
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new App();
        });
        return 0;
    }

    private static bool WaitForPrevious(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.FromSeconds(10));
        }
        catch (AbandonedMutexException)
        {
            // The old instance exited without releasing it; the mutex is ours now.
            return true;
        }
    }
}
