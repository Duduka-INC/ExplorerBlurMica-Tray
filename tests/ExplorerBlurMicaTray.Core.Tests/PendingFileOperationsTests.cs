using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public class PendingFileOperationsTests
{
    private const string Root = @"C:\Program Files\ExplorerBlurMica-Tray";

    [Fact]
    public void RemovesOnlyDeletionsUnderTheFolder()
    {
        string[] entries =
        [
            @"*1\??\C:\Program Files (x86)\Microsoft\EdgeUpdate\1.3.287.9", "",
            @"*1\??\C:\Program Files\ExplorerBlurMica-Tray\ebm\2.1.0\ExplorerBlurMica.dll", "",
            @"\??\C:\Program Files\ExplorerBlurMica-Tray\ebm\2.1.0", "",
            @"*1\??\C:\Program Files\ExplorerBlurMica-Tray", "",
            @"\??\C:\Program Files\ExplorerBlurMica-Tray2\other.dll", "",
            @"\??\C:\Program Files\ExplorerBlurMica-Tray\old.dll", @"!\??\C:\Program Files\ExplorerBlurMica-Tray\new.dll",
            @"*2\??\C:\WINDOWS\fonts\OFFSYM.TTF", "",
        ];

        string[] kept = PendingFileOperations.RemoveDeletionsUnder(entries, Root, out int removed);

        Assert.Equal(3, removed);
        Assert.Equal(
        [
            @"*1\??\C:\Program Files (x86)\Microsoft\EdgeUpdate\1.3.287.9", "",
            @"\??\C:\Program Files\ExplorerBlurMica-Tray2\other.dll", "",
            @"\??\C:\Program Files\ExplorerBlurMica-Tray\old.dll", @"!\??\C:\Program Files\ExplorerBlurMica-Tray\new.dll",
            @"*2\??\C:\WINDOWS\fonts\OFFSYM.TTF", "",
        ], kept);
    }

    [Fact]
    public void NothingToRemoveKeepsEntriesUnchanged()
    {
        string[] entries = [@"\??\C:\Temp\a.tmp", ""];

        string[] kept = PendingFileOperations.RemoveDeletionsUnder(entries, Root, out int removed);

        Assert.Equal(0, removed);
        Assert.Equal(entries, kept);
    }

    [Fact]
    public void OddTrailingEntryIsKept()
    {
        string[] kept = PendingFileOperations.RemoveDeletionsUnder([@"\??\C:\Program Files\ExplorerBlurMica-Tray\x", "", "stray"], Root, out int removed);

        Assert.Equal(1, removed);
        Assert.Equal(["stray"], kept);
    }
}
