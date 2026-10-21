using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public class IniDocumentTests
{
    [Fact]
    public void RoundTripKeepsCommentsUnknownKeysAndOrder()
    {
        const string text = "; comment\r\n[config]\r\neffect=2\r\nfuture=yes\r\n\r\n[dark]\r\nr=0\r\n";

        Assert.Equal(text, IniDocument.Parse(text).ToString());
    }

    [Fact]
    public void GetIsCaseInsensitive()
    {
        var ini = IniDocument.Parse("[Config]\nClearAddress = true\n");

        Assert.Equal("true", ini.Get("config", "clearaddress"));
    }

    [Fact]
    public void SetReplacesExistingValueAndKeepsKeyCasing()
    {
        var ini = IniDocument.Parse("[config]\r\nClearAddress=true\r\n");

        ini.Set("config", "clearAddress", "false");

        Assert.Equal("[config]\r\nClearAddress=false\r\n", ini.ToString());
    }

    [Fact]
    public void SetAddsMissingKeyAfterLastKeyOfItsSection()
    {
        var ini = IniDocument.Parse("[config]\r\neffect=2\r\n\r\n[dark]\r\nr=0\r\n");

        ini.Set("config", "showLine", "true");

        Assert.Equal("[config]\r\neffect=2\r\nshowLine=true\r\n\r\n[dark]\r\nr=0\r\n", ini.ToString());
    }

    [Fact]
    public void SetCreatesMissingSection()
    {
        var ini = IniDocument.Parse("[config]\r\neffect=2\r\n");

        ini.Set("light", "a", "200");

        Assert.Equal("[config]\r\neffect=2\r\n[light]\r\na=200\r\n", ini.ToString());
    }

    [Fact]
    public void CommentsAreNotKeys()
    {
        var ini = IniDocument.Parse("[config]\n;effect=1\neffect=4\n");

        Assert.Equal("4", ini.Get("config", "effect"));
    }
}
