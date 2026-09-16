using BirdScreensaver;
using Xunit;

namespace BirdScreensaver.Tests;

public class ArgsTests
{
    [Fact]
    public void Screensaver_flag() =>
        Assert.Equal(Mode.Screensaver, Args.Parse(["/s"]).Mode);

    [Fact]
    public void Window_and_demo()
    {
        var args = Args.Parse(["--window", "--demo"]);
        Assert.Equal(Mode.Window, args.Mode);
        Assert.True(args.Demo);
    }

    [Fact]
    public void Preview_hwnd()
    {
        var args = Args.Parse(["/p", "12345"]);
        Assert.Equal(Mode.PreviewHwnd, args.Mode);
        Assert.Equal(12345, args.PreviewParent);
    }

    [Fact]
    public void Preview_still()
    {
        var args = Args.Parse(["--preview", "out.png"]);
        Assert.Equal(Mode.PreviewStill, args.Mode);
        Assert.Equal("out.png", args.Output);
    }
}
