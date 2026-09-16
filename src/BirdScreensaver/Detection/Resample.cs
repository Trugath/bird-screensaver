namespace BirdScreensaver.Detection;

internal static class Resample
{
    public static float[] ToMono(byte[] buffer, int bytes, WaveFormatInfo format)
    {
        var floats = new List<float>(bytes / Math.Max(1, format.BytesPerSample));
        if (format.BitsPerSample == 32 && format.IeeeFloat)
        {
            for (var i = 0; i + 4 <= bytes; i += 4)
                floats.Add(BitConverter.ToSingle(buffer, i));
        }
        else if (format.BitsPerSample == 16)
        {
            for (var i = 0; i + 2 <= bytes; i += 2)
                floats.Add(BitConverter.ToInt16(buffer, i) / 32768f);
        }
        else if (format.BitsPerSample == 24)
        {
            for (var i = 0; i + 3 <= bytes; i += 3)
            {
                var sample = buffer[i] | (buffer[i + 1] << 8) | (buffer[i + 2] << 16);
                if ((sample & 0x800000) != 0)
                    sample |= unchecked((int)0xFF000000);
                floats.Add(sample / 8388608f);
            }
        }
        else
        {
            return [];
        }

        if (format.Channels <= 1)
            return [.. floats];

        var mono = new float[floats.Count / format.Channels];
        for (var i = 0; i < mono.Length; i++)
        {
            var sum = 0f;
            for (var c = 0; c < format.Channels; c++)
                sum += floats[i * format.Channels + c];
            mono[i] = sum / format.Channels;
        }
        return mono;
    }

    public static float[] Rate(float[] input, int fromRate, int toRate)
    {
        if (fromRate <= 0 || fromRate == toRate || input.Length == 0)
            return input;
        var n = (int)((long)input.Length * toRate / fromRate);
        var output = new float[Math.Max(1, n)];
        for (var i = 0; i < output.Length; i++)
        {
            var src = (double)i * fromRate / toRate;
            var i0 = (int)src;
            var frac = (float)(src - i0);
            var a = input[Math.Min(i0, input.Length - 1)];
            var b = input[Math.Min(i0 + 1, input.Length - 1)];
            output[i] = a + (b - a) * frac;
        }
        return output;
    }
}

internal readonly record struct WaveFormatInfo(int SampleRate, int Channels, int BitsPerSample, bool IeeeFloat)
{
    public int BytesPerSample => Math.Max(1, BitsPerSample / 8 * Channels);
}
