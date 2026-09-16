namespace BirdScreensaver.Render;

/// <summary>Body mass from AVONET, used to scale birds. Adapted from Fugleramme.</summary>
internal static class Sizes
{
    public const float Exponent = 0.14f;
    private static Dictionary<string, float>? _mass;
    private static float _median = 1;
    private static readonly object Gate = new();

    public static float MassOf(string scientificName)
    {
        Ensure();
        return _mass!.GetValueOrDefault(Artwork.Names.Normalize(scientificName), _median);
    }

    private static void Ensure()
    {
        if (_mass is not null)
            return;
        lock (Gate)
        {
            if (_mass is not null)
                return;
            var map = new Dictionary<string, float>(StringComparer.Ordinal);
            try
            {
                foreach (var line in File.ReadLines(Paths.SizesCsv).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 2)
                        continue;
                    if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var mass))
                    {
                        map[Artwork.Names.Normalize(parts[0])] = mass;
                    }
                }
            }
            catch (IOException)
            {
                // Unknown birds use a unit mass until the table is fetched.
            }
            _mass = map;
            if (map.Count > 0)
            {
                var values = map.Values.OrderBy(v => v).ToList();
                _median = values[values.Count / 2];
            }
        }
    }
}
