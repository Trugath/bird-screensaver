namespace BirdScreensaver.Detection;

internal sealed record RegionChoice(string Id, string Title);

internal static class ModelCatalog
{
    public const string Repo = "tphakala/BirdNET-v3.0-Models";
    public const string Build = "v3.0-preview3.1";
    public const int SampleRate = 32000;
    public const int WindowSamples = 160000;

    public static readonly RegionChoice[] Regions =
    [
        new("western-palearctic", "Western Palearctic"),
        new("british-isles", "British Isles"),
        new("nordic", "Nordic"),
        new("central-europe", "Central Europe"),
        new("southern-europe", "Southern Europe"),
        new("iberia", "Iberia"),
        new("eastern-europe", "Eastern Europe"),
        new("north-america-east", "Eastern North America"),
        new("north-america-west", "Western North America"),
        new("canada-alaska", "Canada and Alaska"),
        new("japan", "Japan"),
        new("australia-east", "Eastern Australia"),
        new("new-zealand", "New Zealand"),
        new("full", "Worldwide"),
    ];

    public static string ModelFileName(string region) =>
        region == "full"
            ? $"birdnet-{Build}-fp32-b1.onnx"
            : $"birdnet-{Build}-{region}-fp32-b1.onnx";

    public static string LabelsFileName(string region) =>
        region == "full"
            ? $"birdnet-{Build}-labels-b1.txt"
            : $"birdnet-{Build}-{region}-labels-b1.txt";

    public static string ModelUrl(string region) =>
        region == "full"
            ? $"https://huggingface.co/{Repo}/resolve/main/full/{ModelFileName(region)}"
            : $"https://huggingface.co/{Repo}/resolve/main/regional/{region}/{ModelFileName(region)}";

    public static string LabelsUrl(string region) =>
        region == "full"
            ? $"https://huggingface.co/{Repo}/resolve/main/full/{LabelsFileName(region)}"
            : $"https://huggingface.co/{Repo}/resolve/main/regional/{region}/{LabelsFileName(region)}";

    public static string LocalModel(string region) => Path.Combine(Paths.ModelsDir, ModelFileName(region));
    public static string LocalLabels(string region) => Path.Combine(Paths.ModelsDir, LabelsFileName(region));

    public static bool IsPresent(string region) =>
        File.Exists(LocalModel(region)) && File.Exists(LocalLabels(region));
}
