using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BirdScreensaver.Detection;

internal sealed class BirdNetSession : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string[] _labels;

    public BirdNetSession(string modelPath, string labelsPath)
    {
        var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2),
        };
        _session = new InferenceSession(modelPath, options);
        _inputName = _session.InputMetadata.Keys.First();
        _labels = File.ReadAllLines(labelsPath)
            .Where(l => l.Length > 0)
            .ToArray();
    }

    public IReadOnlyList<HeardBird> Predict(float[] samples, float threshold)
    {
        var window = new float[ModelCatalog.WindowSamples];
        var copy = Math.Min(samples.Length, window.Length);
        Array.Copy(samples, samples.Length - copy, window, window.Length - copy, copy);

        var tensor = new DenseTensor<float>(window, [1, ModelCatalog.WindowSamples]);
        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, tensor)]);
        var output = results.First(r => r.Name is "predictions" or "output" || r.Name.Contains("pred", StringComparison.OrdinalIgnoreCase));
        var scores = output.AsEnumerable<float>().ToArray();

        var now = DateTimeOffset.Now;
        var hits = new List<HeardBird>();
        var n = Math.Min(scores.Length, _labels.Length);
        for (var i = 0; i < n; i++)
        {
            if (scores[i] < threshold)
                continue;
            var (scientific, _) = SplitLabel(_labels[i]);
            if (!Artwork.Taxa.IsBird(scientific))
                continue;
            hits.Add(new HeardBird(scientific, scores[i], now));
        }
        return hits.OrderByDescending(h => h.Confidence).ToList();
    }

    public static (string Scientific, string Common) SplitLabel(string label)
    {
        var split = label.IndexOf('_');
        return split < 0 ? (label.Trim(), "") : (label[..split].Trim(), label[(split + 1)..].Trim());
    }

    public void Dispose() => _session.Dispose();
}
