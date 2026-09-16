using BirdScreensaver.Detection;
using Xunit;

namespace BirdScreensaver.Tests;

public class ResampleTests
{
    [Fact]
    public void Rate_halves_length()
    {
        var input = Enumerable.Range(0, 100).Select(i => (float)i).ToArray();
        var output = Resample.Rate(input, 48000, 24000);
        Assert.Equal(50, output.Length);
    }

    [Fact]
    public void Mono_from_16bit_stereo()
    {
        var bytes = new byte[] { 0, 64, 0, 64, 0, 0, 0, 0 };
        var samples = Resample.ToMono(bytes, bytes.Length, new WaveFormatInfo(48000, 2, 16, false));
        Assert.Equal(2, samples.Length);
    }
}
