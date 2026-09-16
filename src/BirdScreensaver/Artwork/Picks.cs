using System.Text.Json;

namespace BirdScreensaver.Artwork;

/// <summary>Hold one plate per species while it stays in the window.</summary>
internal sealed class Picks
{
    private readonly string _path;
    private readonly object _gate = new();
    private Dictionary<string, string> _held;

    public Picks(string path)
    {
        _path = path;
        try
        {
            _held = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _held = [];
        }
    }

    public string? Choose(string name, IReadOnlyList<string> variants)
    {
        if (variants.Count == 0)
            return null;
        var byName = variants.ToDictionary(p => Path.GetFileName(p) ?? p, p => p, StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (_held.TryGetValue(name, out var heldName) && byName.TryGetValue(heldName, out var held))
                return held;
            var chosen = variants[Random.Shared.Next(variants.Count)];
            _held[name] = Path.GetFileName(chosen);
            Write();
            return chosen;
        }
    }

    public void Retain(IEnumerable<string> names)
    {
        var keep = names.ToHashSet(StringComparer.Ordinal);
        lock (_gate)
        {
            if (_held.Keys.All(keep.Contains))
                return;
            _held = _held.Where(kv => keep.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            Write();
        }
    }

    private void Write()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var tmp = _path + ".tmp";
        var ordered = _held.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        File.WriteAllText(tmp, JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        File.Move(tmp, _path, overwrite: true);
    }
}
