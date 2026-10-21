using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public class ElevationTests
{
    private const string Sha = "71f678c2356a11a18f9b1e3935d64e035c4f3db71ecf76ff3d27878694f358d5";
    private const string Sid = "S-1-5-21-1004336348-1177238915-682003330-512";

    [Theory]
    [InlineData(new[] { "a", "b" }, "a b")]
    [InlineData(new[] { @"C:\Program Files\x" }, "\"C:\\Program Files\\x\"")]
    [InlineData(new[] { @"C:\dir with space\" }, "\"C:\\dir with space\\\\\"")]
    [InlineData(new[] { "say \"hi\"" }, "\"say \\\"hi\\\"\"")]
    [InlineData(new[] { "" }, "\"\"")]
    public void JoinArgumentsQuotesForCommandLineToArgv(string[] args, string expected)
    {
        Assert.Equal(expected, Elevation.JoinArguments(args));
    }

    [Fact]
    public void InstallArgsParse()
    {
        Assert.True(ElevatedCommands.InstallArgs.TryParse(
            ["install", "--zip", @"C:\x\a.zip", "--sha256", Sha, "--version", "2.1.0", "--user-sid", Sid], out var parsed));
        Assert.Equal(new ElevatedCommands.InstallArgs(@"C:\x\a.zip", Sha, "2.1.0", Sid), parsed);
    }

    [Theory]
    [InlineData("a.zip", Sha, "2.1.0", Sid)]
    [InlineData(@"C:\x\a.zip", "ABC", "2.1.0", Sid)]
    [InlineData(@"C:\x\a.zip", Sha, @"..\..\Windows", Sid)]
    [InlineData(@"C:\x\a.zip", Sha, "2.1.0", "not-a-sid")]
    public void InstallArgsRejectBadValues(string zip, string sha, string version, string sid)
    {
        Assert.False(ElevatedCommands.InstallArgs.TryParse(
            ["install", "--zip", zip, "--sha256", sha, "--version", version, "--user-sid", sid], out _));
    }

    [Fact]
    public void UnknownElevatedCommandIsRejected()
    {
        Assert.True(ElevatedCommands.TryRun([ElevatedCommands.Flag, "format-c"], out int exit));
        Assert.Equal(ElevatedCommands.ExitCodes.BadArguments, exit);
    }

    [Fact]
    public void NormalArgumentsAreNotElevatedCommands()
    {
        Assert.False(ElevatedCommands.TryRun(["status"], out _));
    }

    [Theory]
    [InlineData("2.1.0", "2.0.1", true)]
    [InlineData("2.1.0", "2.1.0", false)]
    [InlineData("2.0.9", "2.1.0", false)]
    [InlineData("2.1.0", null, true)]
    public void IsNewer(string candidate, string? current, bool expected)
    {
        Assert.Equal(expected, EbmVersion.IsNewer(candidate, current));
    }
}
