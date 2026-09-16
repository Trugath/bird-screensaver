using BirdScreensaver.Render;
using Xunit;
using Path = System.IO.Path;
using File = System.IO.File;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BirdScreensaver.Tests;

public class CollageTests
{
    [Fact]
    public void Empty_page_is_paper_sized()
    {
        using var page = Collage.Render([], new Size(640, 400));
        Assert.Equal(640, page.Width);
        Assert.Equal(400, page.Height);
        var px = page[20, 20];
        Assert.InRange(px.R, 220, 255);
        Assert.InRange(px.G, 210, 255);
        Assert.InRange(px.B, 200, 255);
    }

    [Fact]
    public void One_bird_is_drawn()
    {
        var path = Path.Combine(Path.GetTempPath(), "bird-scr-test.webp");
        using (var sprite = new Image<Rgba32>(80, 60, new Rgba32(40, 80, 40, 255)))
            sprite.Save(path);
        try
        {
            using var page = Collage.Render([("Test bird", path)], new Size(640, 400), showNames: false);
            var inked = 0;
            page.ProcessPixelRows(acc =>
            {
                for (var y = 0; y < acc.Height; y++)
                {
                    var row = acc.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        if (Math.Abs(row[x].R - Paper.Target.R) > 20)
                            inked++;
                    }
                }
            });
            Assert.True(inked > 100, "expected the bird body on the page");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
