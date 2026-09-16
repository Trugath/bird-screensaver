namespace BirdScreensaver.Detection;

internal sealed class Hearing
{
    private readonly Dictionary<string, (DateTimeOffset Last, int Count)> _heard = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public void Note(string scientificName, DateTimeOffset when)
    {
        var name = Artwork.Names.Canonical(scientificName);
        lock (_gate)
        {
            if (_heard.TryGetValue(name, out var cur))
                _heard[name] = (when, cur.Count + 1);
            else
                _heard[name] = (when, 1);
        }
    }

    public IReadOnlyList<string> Current(TimeSpan lookback, int limit)
    {
        var cutoff = DateTimeOffset.Now - lookback;
        lock (_gate)
        {
            var live = _heard
                .Where(kv => kv.Value.Last >= cutoff)
                .OrderByDescending(kv => kv.Value.Count)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => kv.Key);
            if (limit > 0)
                live = live.Take(limit);
            return live.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }
    }

    public string Key(TimeSpan lookback, int limit) => string.Join('|', Current(lookback, limit));
}
