using BirdScreensaver.Artwork;
using Xunit;

namespace BirdScreensaver.Tests;

public class NamesTests
{
    [Theory]
    [InlineData("Turdus merula", "turdus-merula")]
    [InlineData("  Parus major ", "parus-major")]
    public void Shape_hyphenates(string input, string expected) =>
        Assert.Equal(expected, Names.Shape(input));

    [Fact]
    public void Normalize_without_alias_file_is_shape() =>
        Assert.Equal("erithacus-rubecula", Names.Normalize("Erithacus rubecula"));
}
