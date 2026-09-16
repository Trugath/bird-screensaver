using BirdScreensaver.Tools;
using Xunit;

namespace BirdScreensaver.Tests;

public class FetchAssetsTests
{
    [Theory]
    [InlineData("fugleramme-main/assets/artwork/classic/birds/foo.webp", "artwork/classic/birds/foo.webp")]
    [InlineData("fugleramme-main/assets/bird_sizes.csv", "bird_sizes.csv")]
    [InlineData("fugleramme-main\\assets\\fonts\\OFL.txt", "fonts/OFL.txt")]
    public void Assets_paths_strip_the_archive_prefix(string entry, string relative)
    {
        Assert.True(FetchAssets.TryAssetsRelative(entry, out var got));
        Assert.Equal(relative, got);
    }

    [Theory]
    [InlineData("fugleramme-main/README.md")]
    [InlineData("fugleramme-main/assets")]
    [InlineData("fugleramme-main/assets/")]
    [InlineData("src/fugleramme/main.py")]
    public void Non_asset_entries_are_skipped(string entry) =>
        Assert.False(FetchAssets.TryAssetsRelative(entry, out _));
}
