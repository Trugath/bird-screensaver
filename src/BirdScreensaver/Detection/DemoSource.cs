namespace BirdScreensaver.Detection;

internal sealed class DemoSource : IBirdSource
{
    private readonly IReadOnlyList<string> _species;
    private readonly Hearing _hearing;
    private CancellationTokenSource? _cts;

    public event Action<string>? Status;
    public event Action<IReadOnlyList<HeardBird>>? Heard;

    public DemoSource(IEnumerable<string> species, Hearing hearing)
    {
        _species = species.ToList();
        _hearing = hearing;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => Loop(_cts.Token));
    }

    private async Task Loop(CancellationToken token)
    {
        if (_species.Count == 0)
        {
            Status?.Invoke("No plates found. Run fetch-assets.");
            return;
        }

        Status?.Invoke("Demo garden");
        var garden = PickGarden();
        foreach (var name in garden)
            _hearing.Note(name, DateTimeOffset.Now);
        Heard?.Invoke(garden.Select(n => new HeardBird(n, 1, DateTimeOffset.Now)).ToList());

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(40), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            var next = _species[Random.Shared.Next(_species.Count)];
            _hearing.Note(next, DateTimeOffset.Now);
            Heard?.Invoke([new HeardBird(next, 1, DateTimeOffset.Now)]);
        }
    }

    private List<string> PickGarden()
    {
        var take = Math.Clamp(_species.Count, 1, Math.Min(8, _species.Count));
        return _species.OrderBy(_ => Random.Shared.Next()).Take(Math.Max(3, take / 8 + 3)).ToList();
    }

    public void Dispose() => _cts?.Cancel();
}
