using System.Text.Json;
using System.Text.RegularExpressions;

namespace BirdScreensaver.Artwork;

/// <summary>
/// Map a detector's scientific name to a plate filename.
/// Adapted from Fugleramme (MIT).
/// </summary>
internal static class Names
{
    public const string Birds = "birds";
    public const string Perches = "perches";
    private static readonly string[] Suffixes = [".webp", ".png"];
    private static readonly Regex Numbered = new(@"-(\d+)$", RegexOptions.Compiled);
    private static readonly object Gate = new();
    private static Dictionary<string, string>? _currentName;
    private static Dictionary<string, string>? _currentKey;
    private static Dictionary<string, string>? _legacyKey;

    public static string Shape(string scientificName) =>
        scientificName.Trim().ToLowerInvariant().Replace(' ', '-');

    public static string Normalize(string scientificName)
    {
        EnsureAliases();
        var key = Shape(scientificName);
        return _currentKey!.GetValueOrDefault(key, key);
    }

    public static string Canonical(string scientificName)
    {
        EnsureAliases();
        return _currentName!.GetValueOrDefault(Shape(scientificName), scientificName.Trim());
    }

    public static IEnumerable<string> ArtworkKeys(string scientificName)
    {
        EnsureAliases();
        var key = Normalize(scientificName);
        yield return key;
        if (_legacyKey!.TryGetValue(key, out var legacy))
            yield return legacy;
    }

    public static IReadOnlyList<string> AvailableStyles(string imagesDir)
    {
        if (!Directory.Exists(imagesDir))
            return [];
        return Directory.GetDirectories(imagesDir)
            .Where(dir => HoldsArtwork(Path.Combine(dir, Birds)))
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    public static string Resolve(string requested, string imagesDir)
    {
        var available = AvailableStyles(imagesDir);
        if (available.Contains(requested))
            return requested;
        return available.FirstOrDefault() ?? "";
    }

    public static List<string> VariantsFor(string scientificName, string imagesDir, string style)
    {
        if (string.IsNullOrEmpty(style))
            return [];
        var folder = Path.Combine(imagesDir, style, Birds);
        if (!Directory.Exists(folder))
            return [];

        var variants = new List<string>();
        foreach (var key in ArtworkKeys(scientificName))
        {
            var numbered = new Regex($"^{Regex.Escape(key)}-(\\d+)$");
            var baseFile = ArtworkFile(folder, key);
            if (baseFile is not null)
                variants.Add(baseFile);
            var rest = new List<(int N, string Path)>();
            foreach (var (stem, path) in ByStem(folder, key + "-*"))
            {
                var m = numbered.Match(stem);
                if (m.Success)
                    rest.Add((int.Parse(m.Groups[1].Value), path));
            }
            rest.Sort((a, b) => a.N.CompareTo(b.N));
            variants.AddRange(rest.Select(x => x.Path));
        }
        return variants;
    }

    public static HashSet<string> DrawableKeys(string imagesDir, string style)
    {
        if (string.IsNullOrEmpty(style))
            return [];
        var folder = Path.Combine(imagesDir, style, Birds);
        return ByStem(folder, "*")
            .Select(kv => Normalize(Numbered.Replace(kv.Key, "")))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static List<string> PerchesFor(string imagesDir, string style)
    {
        if (string.IsNullOrEmpty(style))
            return [];
        return ByStem(Path.Combine(imagesDir, style, Perches), "*")
            .Values
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    private static bool HoldsArtwork(string folder) =>
        Directory.Exists(folder) && Suffixes.Any(s => Directory.EnumerateFiles(folder, "*" + s).Any());

    private static string? ArtworkFile(string folder, string stem) =>
        Suffixes.Select(s => Path.Combine(folder, stem + s)).FirstOrDefault(File.Exists);

    private static Dictionary<string, string> ByStem(string folder, string pattern)
    {
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folder))
            return found;
        foreach (var suffix in Suffixes.Reverse())
        {
            foreach (var path in Directory.EnumerateFiles(folder, pattern + suffix))
                found[Path.GetFileNameWithoutExtension(path)] = path;
        }
        return found;
    }

    private static void EnsureAliases()
    {
        if (_currentName is not null)
            return;
        lock (Gate)
        {
            if (_currentName is not null)
                return;
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Paths.AliasesJson));
                if (loaded is not null)
                {
                    foreach (var (legacy, current) in loaded)
                    {
                        if (Shape(legacy) != Shape(current))
                            names[Shape(legacy)] = current.Trim();
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Missing alias map is fine: artwork keys stay as reported.
            }
            _currentName = names;
            _currentKey = names.ToDictionary(kv => kv.Key, kv => Shape(kv.Value), StringComparer.Ordinal);
            _legacyKey = _currentKey
                .GroupBy(kv => kv.Value, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);
        }
    }
}
