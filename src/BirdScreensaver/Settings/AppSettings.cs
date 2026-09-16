using System.Text.Json;
using System.Text.Json.Serialization;

namespace BirdScreensaver.Settings;

internal sealed class AppSettings
{
    public string? Microphone { get; set; }
    public string Region { get; set; } = "western-palearctic";
    public float Confidence { get; set; } = 0.65f;
    public float LookbackMinutes { get; set; } = 30;
    public int MaxBirds { get; set; }
    public bool ShowNames { get; set; } = true;
    public bool Demo { get; set; }
    public string Style { get; set; } = "classic";
    public string Font { get; set; } = "gentium";
    public string LabelSize { get; set; } = "medium";

    public TimeSpan Lookback => TimeSpan.FromMinutes(Math.Max(1, LookbackMinutes));

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static AppSettings Load()
    {
        try
        {
            var text = File.ReadAllText(Paths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(text, Json) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var tmp = Paths.SettingsFile + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json) + Environment.NewLine);
        File.Move(tmp, Paths.SettingsFile, overwrite: true);
    }
}
