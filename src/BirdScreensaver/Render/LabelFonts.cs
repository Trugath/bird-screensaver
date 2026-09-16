using SixLabors.Fonts;
using File = System.IO.File;
using Path = System.IO.Path;

namespace BirdScreensaver.Render;

internal static class LabelFonts
{
    public const string DefaultFont = "gentium";
    public const string DefaultLabelSize = "medium";
    public const int MinLabelPx = 11;

    public static readonly Dictionary<string, (string Title, string File)> Faces = new(StringComparer.Ordinal)
    {
        ["gentium"] = ("Gentium Book Plus", "gentiumbookplus/GentiumBookPlus-Italic.ttf"),
        ["garamond"] = ("EB Garamond", "ebgaramond/EBGaramond-Italic.ttf"),
        ["cormorant"] = ("Cormorant Garamond", "cormorantgaramond/CormorantGaramond-Italic.ttf"),
        ["baskerville"] = ("Libre Baskerville", "librebaskerville/LibreBaskerville-Italic.ttf"),
        ["playfair"] = ("Playfair Display", "playfairdisplay/PlayfairDisplay-Italic.ttf"),
        ["alegreya"] = ("Alegreya", "alegreya/Alegreya-Italic.ttf"),
        ["bitter"] = ("Bitter", "bitter/Bitter-Italic.ttf"),
    };

    public static readonly Dictionary<string, (string Title, float Scale)> LabelSizes = new(StringComparer.Ordinal)
    {
        ["small"] = ("Small", 0.024f),
        ["medium"] = ("Medium", 0.032f),
        ["large"] = ("Large", 0.042f),
        ["xlarge"] = ("Extra large", 0.055f),
    };

    public static Font Load(string key, int size)
    {
        var file = Faces.GetValueOrDefault(key, Faces[DefaultFont]).File;
        var path = Path.Combine(Paths.Fonts, file);
        if (File.Exists(path))
        {
            var collection = new FontCollection();
            var family = collection.Add(path);
            return family.CreateFont(size, FontStyle.Italic);
        }

        if (SystemFonts.TryGet("Times New Roman", out var times))
            return times.CreateFont(size, FontStyle.Italic);
        return SystemFonts.Families.First().CreateFont(size, FontStyle.Italic);
    }

    public static int LabelPx(int width, int height, string sizeKey)
    {
        var scale = LabelSizes.GetValueOrDefault(sizeKey, LabelSizes[DefaultLabelSize]).Scale;
        return Math.Max(MinLabelPx, (int)Math.Round(Math.Min(width, height) * scale));
    }
}
