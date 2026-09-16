using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BirdScreensaver.Detection;

internal static class Microphones
{
    public static IReadOnlyList<string> List()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => d.FriendlyName)
            .ToList();
    }

    public static MMDevice? Find(string? name)
    {
        using var enumerator = new MMDeviceEnumerator();
        if (string.IsNullOrWhiteSpace(name))
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .FirstOrDefault(d => string.Equals(d.FriendlyName, name, StringComparison.OrdinalIgnoreCase))
            ?? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
    }

    public static WasapiCapture Open(string? name)
    {
        var device = Find(name) ?? throw new InvalidOperationException("No microphone found.");
        return new WasapiCapture(device);
    }
}
