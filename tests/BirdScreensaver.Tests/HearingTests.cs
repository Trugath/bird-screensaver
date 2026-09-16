using BirdScreensaver.Detection;
using Xunit;

namespace BirdScreensaver.Tests;

public class HearingTests
{
    [Fact]
    public void Current_drops_stale_birds()
    {
        var hearing = new Hearing();
        hearing.Note("Turdus merula", DateTimeOffset.Now.AddMinutes(-40));
        hearing.Note("Parus major", DateTimeOffset.Now);
        var now = hearing.Current(TimeSpan.FromMinutes(30), 0);
        Assert.Equal(["Parus major"], now);
    }

    [Fact]
    public void Current_caps_by_count()
    {
        var hearing = new Hearing();
        hearing.Note("A", DateTimeOffset.Now);
        hearing.Note("A", DateTimeOffset.Now);
        hearing.Note("B", DateTimeOffset.Now);
        var names = hearing.Current(TimeSpan.FromMinutes(30), 1);
        Assert.Equal(["A"], names);
    }

    [Fact]
    public void Key_is_stable_for_same_set()
    {
        var hearing = new Hearing();
        hearing.Note("Parus major", DateTimeOffset.Now);
        hearing.Note("Turdus merula", DateTimeOffset.Now);
        var lookback = TimeSpan.FromMinutes(30);
        Assert.Equal(hearing.Key(lookback, 0), hearing.Key(lookback, 0));
    }
}
