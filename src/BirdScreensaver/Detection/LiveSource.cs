using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BirdScreensaver.Detection;

internal sealed class LiveSource : IBirdSource
{
    private readonly Settings.AppSettings _settings;
    private readonly Hearing _hearing;
    private WasapiCapture? _capture;
    private BirdNetSession? _session;
    private readonly object _gate = new();
    private readonly List<float> _pending = [];

    public event Action<string>? Status;
    public event Action<IReadOnlyList<HeardBird>>? Heard;

    public LiveSource(Settings.AppSettings settings, Hearing hearing)
    {
        _settings = settings;
        _hearing = hearing;
    }

    public void Start()
    {
        _ = Task.Run(StartCore);
    }

    private void StartCore()
    {
        try
        {
            if (!ModelCatalog.IsPresent(_settings.Region))
            {
                Status?.Invoke("No BirdNET model. Open settings and download one, or enable demo.");
                return;
            }

            Status?.Invoke("Loading BirdNET…");
            _session = new BirdNetSession(
                ModelCatalog.LocalModel(_settings.Region),
                ModelCatalog.LocalLabels(_settings.Region));

            _capture = Microphones.Open(_settings.Microphone);
            _capture.DataAvailable += OnData;
            _capture.RecordingStopped += (_, e) =>
            {
                if (e.Exception is not null)
                    Status?.Invoke(e.Exception.Message);
            };
            _capture.StartRecording();
            Status?.Invoke("Listening");
        }
        catch (Exception ex)
        {
            Status?.Invoke(ex.Message);
        }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        if (_capture is null || _session is null)
            return;
        var format = _capture.WaveFormat;
        var info = new WaveFormatInfo(
            format.SampleRate,
            format.Channels,
            format.BitsPerSample,
            format.Encoding == WaveFormatEncoding.IeeeFloat);
        var mono = Resample.ToMono(e.Buffer, e.BytesRecorded, info);
        var at32 = Resample.Rate(mono, info.SampleRate, ModelCatalog.SampleRate);

        float[]? window = null;
        lock (_gate)
        {
            _pending.AddRange(at32);
            var need = ModelCatalog.WindowSamples;
            if (_pending.Count >= need)
            {
                window = _pending.TakeLast(need).ToArray();
                var keep = need / 2;
                _pending.RemoveRange(0, _pending.Count - keep);
            }
        }
        if (window is null)
            return;

        try
        {
            var hits = _session.Predict(window, _settings.Confidence);
            if (hits.Count == 0)
                return;
            foreach (var hit in hits)
                _hearing.Note(hit.ScientificName, hit.At);
            Heard?.Invoke(hits);
        }
        catch (Exception ex)
        {
            Status?.Invoke(ex.Message);
        }
    }

    public void Dispose()
    {
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnData;
            try { _capture.StopRecording(); } catch (Exception) { /* already stopped */ }
            _capture.Dispose();
        }
        _session?.Dispose();
    }
}
