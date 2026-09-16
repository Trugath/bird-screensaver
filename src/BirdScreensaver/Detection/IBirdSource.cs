namespace BirdScreensaver.Detection;

internal interface IBirdSource : IDisposable
{
    event Action<string>? Status;
    event Action<IReadOnlyList<HeardBird>>? Heard;
    void Start();
}

internal readonly record struct HeardBird(string ScientificName, float Confidence, DateTimeOffset At);
