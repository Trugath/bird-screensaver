namespace BirdScreensaver.Artwork;

/// <summary>Which detections are birds. Adapted from Fugleramme (MIT).</summary>
internal static class Taxa
{
    private static readonly HashSet<string> NonBirdGenera = new(StringComparer.Ordinal)
    {
        "dog", "engine", "environmental", "fireworks", "gun", "human", "noise", "power", "siren",
        "acris", "anaxyrus", "dryophytes", "eleutherodactylus", "gastrophryne", "hyliola", "incilius",
        "lithobates", "pseudacris", "scaphiopus", "spea",
        "allonemobius", "amblycorypha", "anaxipha", "apis", "atlanticus", "conocephalus", "cyrtoxipha",
        "eunemobius", "gryllus", "hapithus", "microcentrum", "miogryllus", "neoconocephalus", "neonemobius",
        "oecanthus", "orchelimum", "orocharis", "phyllopalpus", "pterophylla", "scudderia",
        "alouatta", "canis", "odocoileus", "sciurus", "tamias", "tamiasciurus",
    };

    private static Dictionary<string, string>? _labels;
    private static readonly object Gate = new();

    public static bool IsBird(string scientificName)
    {
        var key = Names.Normalize(scientificName);
        var known = Labels();
        if (known.Count > 0 && !known.ContainsKey(key))
            return false;
        var genus = key.Split('-', 2)[0];
        return !NonBirdGenera.Contains(genus);
    }

    public static string CommonOf(string scientificName) =>
        Labels().GetValueOrDefault(Names.Normalize(scientificName), "");

    private static Dictionary<string, string> Labels()
    {
        if (_labels is not null)
            return _labels;
        lock (Gate)
        {
            if (_labels is not null)
                return _labels;
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                foreach (var line in File.ReadLines(Paths.LabelsFile))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var parts = line.Split('_', 2);
                    map[Names.Normalize(parts[0])] = parts.Length > 1 ? parts[1] : "";
                }
            }
            catch (IOException)
            {
                // Fail open: a missing allowlist must not blank the page.
            }
            _labels = map;
            return _labels;
        }
    }
}
