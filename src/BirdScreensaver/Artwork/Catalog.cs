namespace BirdScreensaver.Artwork;

internal sealed class Catalog
{
    private readonly string _imagesDir;
    private readonly Picks _picks;

    public Catalog(string imagesDir, Picks picks)
    {
        _imagesDir = imagesDir;
        _picks = picks;
    }

    public string Style { get; private set; } = "";

    public void UseStyle(string requested) => Style = Names.Resolve(requested, _imagesDir);

    public IReadOnlyCollection<string> Drawable => Names.DrawableKeys(_imagesDir, Style);

    public IReadOnlyList<string> Perches => Names.PerchesFor(_imagesDir, Style);

    public string? ImageFor(string scientificName) =>
        _picks.Choose(scientificName, Names.VariantsFor(scientificName, _imagesDir, Style));

    public IReadOnlyList<(string Name, string? Path)> Entries(IEnumerable<string> names)
    {
        var list = names
            .Select(Names.Canonical)
            .Distinct(StringComparer.Ordinal)
            .Where(n => Drawable.Contains(Names.Normalize(n)))
            .OrderBy(n => n, StringComparer.Ordinal)
            .Select(n => (n, ImageFor(n)))
            .ToList();
        _picks.Retain(list.Select(e => e.n));
        return list;
    }

    public bool HasArtwork => Drawable.Count > 0;
}
