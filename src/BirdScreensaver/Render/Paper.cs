using Path = System.IO.Path;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BirdScreensaver.Render;

/// <summary>Aged paper and halo matching. Adapted from Fugleramme (MIT).</summary>
internal static class Paper
{
    public static readonly Rgb24 Target = new(242, 237, 226);
    public const int Feather = 5;
    public const int Pad = 16;

    public static Image<Rgb24> Texture(int width, int height, int seed = 0)
    {
        var rng = new Random(seed);
        var fine = FineGrain(width, height, rng, 2.6f, 0.6f);
        var coarseW = width / 16 + 1;
        var coarseH = height / 16 + 1;
        var coarse = new Image<L8>(coarseW, coarseH);
        coarse.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new L8((byte)Math.Clamp(128 + NextGaussian(rng) * 1.4f, 0, 255));
            }
        });
        coarse.Mutate(c => c.Resize(width, height, KnownResamplers.Bicubic));

        var page = new Image<Rgb24>(width, height);
        page.ProcessPixelRows(fine, coarse, (pageAcc, fineAcc, coarseAcc) =>
        {
            for (var y = 0; y < pageAcc.Height; y++)
            {
                var dest = pageAcc.GetRowSpan(y);
                var f = fineAcc.GetRowSpan(y);
                var m = coarseAcc.GetRowSpan(y);
                for (var x = 0; x < dest.Length; x++)
                {
                    var n = (f[x].PackedValue - 128) + (m[x].PackedValue - 128);
                    dest[x] = new Rgb24(
                        (byte)Math.Clamp(Target.R + n, 0, 255),
                        (byte)Math.Clamp(Target.G + n, 0, 255),
                        (byte)Math.Clamp(Target.B + n, 0, 255));
                }
            }
        });
        coarse.Dispose();
        fine.Dispose();
        return page;
    }

    public static Image<Rgba32> ProcessSprite(Image<Rgba32> sprite, bool textured = true, int seed = 0)
    {
        var w = sprite.Width;
        var h = sprite.Height;
        var opaque = new bool[h, w];
        sprite.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    opaque[y, x] = row[x].A > 24;
            }
        });

        var nearEdge = DilateTransparent(opaque, 4);
        var ring = new List<(int X, int Y)>();
        var paperSamples = new List<Rgb24>();
        sprite.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (nearEdge[y, x] && opaque[y, x])
                    {
                        ring.Add((x, y));
                        paperSamples.Add(new Rgb24(row[x].R, row[x].G, row[x].B));
                    }
                }
            }
        });

        var paper = paperSamples.Count > 50 ? Median(paperSamples) : Target;
        var deltaR = Target.R - paper.R;
        var deltaG = Target.G - paper.G;
        var deltaB = Target.B - paper.B;
        var paperPx = new bool[h, w];

        sprite.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (!opaque[y, x])
                        continue;
                    var px = row[x];
                    var dist = Math.Max(Math.Abs(px.R - paper.R), Math.Max(Math.Abs(px.G - paper.G), Math.Abs(px.B - paper.B)));
                    var sat = Math.Max(px.R, Math.Max(px.G, px.B)) - Math.Min(px.R, Math.Min(px.G, px.B));
                    if (dist < 50 && sat < 55 && Math.Max(px.R, Math.Max(px.G, px.B)) > 160)
                    {
                        paperPx[y, x] = true;
                        row[x] = new Rgba32(
                            (byte)Math.Clamp(px.R + deltaR, 0, 255),
                            (byte)Math.Clamp(px.G + deltaG, 0, 255),
                            (byte)Math.Clamp(px.B + deltaB, 0, 255),
                            px.A);
                    }
                }
            }
        });

        foreach (var (x, y) in ring)
        {
            if (!paperPx[y, x])
                continue;
            sprite[x, y] = new Rgba32(Target.R, Target.G, Target.B, sprite[x, y].A);
        }

        var padded = new Image<Rgba32>(w + Pad * 2, h + Pad * 2, new Rgba32(Target.R, Target.G, Target.B, 0));
        padded.ProcessPixelRows(sprite, (destAcc, srcAcc) =>
        {
            for (var y = 0; y < srcAcc.Height; y++)
            {
                var src = srcAcc.GetRowSpan(y);
                var dest = destAcc.GetRowSpan(y + Pad);
                src.CopyTo(dest[Pad..(Pad + src.Length)]);
            }
        });
        padded.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A <= 24)
                        row[x] = new Rgba32(Target.R, Target.G, Target.B, row[x].A);
                }
            }
        });

        if (textured)
        {
            var rng = new Random(seed);
            var grain = FineGrain(padded.Width, padded.Height, rng, 2.6f, 0.6f);
            padded.ProcessPixelRows(grain, (destAcc, grainAcc) =>
            {
                for (var y = 0; y < destAcc.Height; y++)
                {
                    var dest = destAcc.GetRowSpan(y);
                    var g = grainAcc.GetRowSpan(y);
                    for (var x = 0; x < dest.Length; x++)
                    {
                        var inSprite = y >= Pad && y < Pad + h && x >= Pad && x < Pad + w;
                        var isPaper = dest[x].A <= 24 || (inSprite && paperPx[y - Pad, x - Pad]);
                        if (!isPaper)
                            continue;
                        var n = g[x].PackedValue - 128;
                        dest[x] = new Rgba32(
                            (byte)Math.Clamp(dest[x].R + n, 0, 255),
                            (byte)Math.Clamp(dest[x].G + n, 0, 255),
                            (byte)Math.Clamp(dest[x].B + n, 0, 255),
                            dest[x].A);
                    }
                }
            });
            grain.Dispose();
        }

        using var alpha = padded.Clone(c => c.GaussianBlur(Feather));
        padded.ProcessPixelRows(alpha, (destAcc, blurAcc) =>
        {
            for (var y = 0; y < destAcc.Height; y++)
            {
                var dest = destAcc.GetRowSpan(y);
                var blur = blurAcc.GetRowSpan(y);
                for (var x = 0; x < dest.Length; x++)
                    dest[x].A = blur[x].A;
            }
        });
        return padded;
    }

    private static Image<L8> FineGrain(int width, int height, Random rng, float sigma, float blur)
    {
        var img = new Image<L8>(width, height);
        img.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new L8((byte)Math.Clamp(128 + NextGaussian(rng) * sigma, 0, 255));
            }
        });
        img.Mutate(c => c.GaussianBlur(blur));
        return img;
    }

    private static bool[,] DilateTransparent(bool[,] opaque, int radius)
    {
        var h = opaque.GetLength(0);
        var w = opaque.GetLength(1);
        var near = new bool[h, w];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (opaque[y, x])
                    continue;
                var y0 = Math.Max(0, y - radius);
                var y1 = Math.Min(h - 1, y + radius);
                var x0 = Math.Max(0, x - radius);
                var x1 = Math.Min(w - 1, x + radius);
                for (var yy = y0; yy <= y1; yy++)
                {
                    for (var xx = x0; xx <= x1; xx++)
                        near[yy, xx] = true;
                }
            }
        }
        return near;
    }

    private static Rgb24 Median(List<Rgb24> samples)
    {
        samples.Sort((a, b) => (a.R + a.G + a.B).CompareTo(b.R + b.G + b.B));
        return samples[samples.Count / 2];
    }

    private static float NextGaussian(Random rng)
    {
        var u1 = 1f - rng.NextSingle();
        var u2 = rng.NextSingle();
        return MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);
    }
}
