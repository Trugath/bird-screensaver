using SixLabors.ImageSharp;
using BirdScreensaver.Artwork;
using BirdScreensaver.Detection;
using BirdScreensaver.Render;
using BirdScreensaver.Settings;

namespace BirdScreensaver.Tools;

internal static class PreviewCommand
{
    public static int Run(string output, AppSettings settings, bool demo)
    {
        var catalog = new Catalog(Paths.Artwork, new Picks(Paths.PicksFile));
        catalog.UseStyle(settings.Style);
        IReadOnlyList<(string Name, string? Path)> entries;
        if (demo || catalog.HasArtwork)
        {
            var names = catalog.Drawable.OrderBy(_ => Random.Shared.Next()).Take(6).ToList();
            entries = catalog.Entries(names);
        }
        else
        {
            entries = [];
        }
        using var page = Collage.Render(
            entries,
            new SixLabors.ImageSharp.Size(1600, 1000),
            settings.ShowNames,
            settings.Font,
            settings.LabelSize,
            catalog.Perches);
        page.SaveAsPng(output);
        return 0;
    }
}
