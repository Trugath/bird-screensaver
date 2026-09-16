using System.Security.Cryptography;
using System.Text;
using File = System.IO.File;
using Path = System.IO.Path;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BirdScreensaver.Render;

/// <summary>
/// Pack birds by silhouette onto one sheet of paper.
/// Adapted from Fugleramme (MIT) by Arne Giacomo Munthe-Kaas.
/// </summary>
internal static class Collage
{
    public static readonly Size DefaultResolution = new(1280, 800);
    private const int PackShort = 1200;
    private const float Margin = 0.04f;
    private const int AlphaCutoff = 24;
    private const int OverlapPx = 2;
    private const int Step = 6;
    private const int Attempts = 20;

    public static Image<Rgb24> Render(
        IReadOnlyList<(string Name, string? Path)> entries,
        Size resolution,
        bool showNames = true,
        string fontKey = LabelFonts.DefaultFont,
        string labelSize = LabelFonts.DefaultLabelSize,
        IReadOnlyList<string>? perches = null)
    {
        var canvas = Page.Blank(resolution.Width, resolution.Height, textured: true);
        var kept = entries.Where(e => e.Path is not null && File.Exists(e.Path)).ToList();
        if (kept.Count == 0)
        {
            Page.DrawPerch(canvas, perches ?? [], DateTime.Today.DayOfYear);
            return canvas;
        }

        var arts = kept.Select(e => Page.Trim(e.Path!)).ToList();
        var scale = Math.Min(resolution.Width, resolution.Height) / (float)PackShort;
        var width = (int)Math.Round(resolution.Width / scale);
        var height = (int)Math.Round(resolution.Height / scale);
        var names = kept.Select(e => e.Name).ToList();
        var flips = names.Select(Flip).ToList();
        var namePx = LabelFonts.LabelPx(width, height, labelSize);

        var (placed, usedPx) = Place(arts, names, flips, width, height, showNames ? fontKey : null, namePx);

        foreach (var p in placed)
        {
            using var art = Scaled(arts[p.Index], Math.Max(1, (int)Math.Round(p.Dim * scale)), flips[p.Index]);
            using var proc = Paper.ProcessSprite(art);
            var at = At(p.At, scale);
            canvas.Mutate(c => c.DrawImage(proc, new Point(at.X - Paper.Pad, at.Y - Paper.Pad), 1f));
        }

        if (usedPx > 0 && showNames)
        {
            var font = LabelFonts.Load(fontKey, Math.Max(1, (int)Math.Round(usedPx * scale)));
            foreach (var p in placed)
            {
                if (p.LabelAt is null)
                    continue;
                using var mask = Page.TextMask(names[p.Index], font);
                var at = At(p.LabelAt.Value, scale);
                var centred = at.X + (int)Math.Round(p.LabelW * scale - mask.Width) / 2;
                Page.Stamp(canvas, mask, centred, at.Y);
            }
        }

        foreach (var art in arts)
            art.Dispose();
        return canvas;
    }

    private readonly record struct Placed(int Index, int Dim, Point At, Point? LabelAt, int LabelW);

    private readonly record struct Sprite(int Index, int Dim, bool[,] Mask, Point ArtAt, Point? LabelAt, int LabelW);

    private static (List<Placed> Placed, int UsedPx) Place(
        List<Image<Rgba32>> arts,
        List<string> names,
        List<bool> flips,
        int width,
        int height,
        string? fontKey,
        int namePx)
    {
        var weights = SizeWeights(names);
        var order = Enumerable.Range(0, names.Count).OrderByDescending(i => weights[i]).ToList();
        var baseDim = Math.Min(
            MathF.Sqrt(width * height * 1.5f / weights.Sum(w => w * w)),
            Math.Min(width, height) * 0.7f / weights.Max());
        var margin = (int)Math.Round(Math.Min(width, height) * Margin);
        var boxW = width - 2 * margin;
        var boxH = height - 2 * margin;
        var alphas = arts.Select(Alpha).ToList();

        var (packed, usedPx) = Layout(names, alphas, order, weights, flips, baseDim, boxW, boxH, fontKey, namePx);
        if (packed is null && fontKey is not null)
            (packed, usedPx) = Layout(names, alphas, order, weights, flips, baseDim, boxW, boxH, null, namePx);

        var result = new List<Placed>();
        if (packed is not null)
        {
            packed = Center(packed, boxW, boxH);
            foreach (var (sprite, x, y) in packed)
            {
                result.Add(new Placed(
                    sprite.Index,
                    sprite.Dim,
                    new Point(x + margin + sprite.ArtAt.X, y + margin + sprite.ArtAt.Y),
                    sprite.LabelAt is { } la ? new Point(x + margin + la.X, y + margin + la.Y) : null,
                    sprite.LabelW));
            }
        }
        return (result, usedPx);
    }

    private static (List<(Sprite Sprite, int X, int Y)>? Packed, int UsedPx) Layout(
        List<string> names,
        List<Image<L8>> alphas,
        List<int> order,
        List<float> weights,
        List<bool> flips,
        float baseDim,
        int width,
        int height,
        string? fontKey,
        int namePx)
    {
        List<Image<L8>> labels = [];
        var lastPx = 0;
        var gap = 0;
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var shrink = MathF.Pow(0.9f, attempt);
            if (fontKey is not null)
            {
                var px = Math.Max(LabelFonts.MinLabelPx, (int)Math.Round(namePx * shrink));
                if (px != lastPx)
                {
                    foreach (var old in labels)
                        old.Dispose();
                    var font = LabelFonts.Load(fontKey, px);
                    labels = order.Select(i => Page.TextMask(names[i], font)).ToList();
                    lastPx = px;
                    gap = (int)Math.Round(px * 0.35);
                }
            }

            var sprites = new List<Sprite>();
            foreach (var n in Enumerable.Range(0, order.Count))
            {
                var i = order[n];
                var dim = Math.Max(24, (int)(baseDim * shrink * weights[i]));
                using var scaled = ScaledGray(alphas[i], dim, flips[i]);
                var mask = Footprint(scaled);
                sprites.Add(fontKey is not null
                    ? WithLabel(i, dim, mask, labels[n], gap)
                    : new Sprite(i, dim, mask, Point.Empty, null, 0));
            }

            var packed = Pack(sprites, width, height);
            if (packed is not null)
            {
                foreach (var a in alphas)
                    a.Dispose();
                foreach (var l in labels)
                    l.Dispose();
                return (packed, lastPx);
            }
        }
        foreach (var a in alphas)
            a.Dispose();
        foreach (var l in labels)
            l.Dispose();
        return (null, 0);
    }

    private static List<(Sprite Sprite, int X, int Y)>? Pack(List<Sprite> sprites, int width, int height)
    {
        var occ = new bool[height, width];
        var placed = new List<(Sprite, int, int)>();
        var maxR = MathF.Sqrt(width * (float)width + height * (float)height);
        foreach (var sprite in sprites)
        {
            var h = sprite.Mask.GetLength(0);
            var w = sprite.Mask.GetLength(1);
            var probes = Probes(sprite.Mask);
            int? sx = null, sy = null;
            foreach (var (px, py) in Spiral(width / 2f, height / 2f, maxR))
            {
                var x = (int)(px - w / 2f);
                var y = (int)(py - h / 2f);
                if (x < 0 || y < 0 || x + w > width || y + h > height)
                    continue;
                if (probes.Any(p => RowCollides(occ, y + p.Row, x, w, p.Bits)))
                    continue;
                if (!Collides(occ, sprite.Mask, x, y))
                {
                    sx = x;
                    sy = y;
                    break;
                }
            }
            if (sx is null || sy is null)
                return null;
            StampMask(occ, sprite.Mask, sx.Value, sy.Value);
            placed.Add((sprite, sx.Value, sy.Value));
        }
        return placed;
    }

    private static List<(Sprite Sprite, int X, int Y)> Center(List<(Sprite Sprite, int X, int Y)> placed, int width, int height)
    {
        var xs0 = placed.Min(p => p.X);
        var ys0 = placed.Min(p => p.Y);
        var xs1 = placed.Max(p => p.X + p.Sprite.Mask.GetLength(1));
        var ys1 = placed.Max(p => p.Y + p.Sprite.Mask.GetLength(0));
        var dx = (width - (xs1 - xs0)) / 2 - xs0;
        var dy = (height - (ys1 - ys0)) / 2 - ys0;
        return placed.Select(p => (p.Sprite, p.X + dx, p.Y + dy)).ToList();
    }

    private static Sprite WithLabel(int index, int dim, bool[,] artMask, Image<L8> label, int gap)
    {
        var ah = artMask.GetLength(0);
        var aw = artMask.GetLength(1);
        var lw = label.Width;
        var lh = label.Height;
        var cols = new List<int>();
        for (var y = 0; y < ah; y++)
        {
            for (var x = 0; x < aw; x++)
            {
                if (artMask[y, x])
                    cols.Add(x);
            }
        }
        var centre = cols.Count > 0 ? cols.Average() : aw / 2f;
        var offset = (int)Math.Round(centre - lw / 2f);
        var left = Math.Min(0, offset);
        var width = Math.Max(aw, offset + lw) - left;
        var ax = -left;
        var lx = offset - left;
        var underMax = 0;
        for (var y = 0; y < ah; y++)
        {
            var x0 = Math.Max(0, lx - ax);
            var x1 = Math.Min(aw, lx - ax + lw);
            for (var x = x0; x < x1; x++)
            {
                if (artMask[y, x])
                    underMax = y + 1;
            }
        }
        var top = underMax + gap;
        var height = Math.Max(ah, top + lh);
        var mask = new bool[height, width];
        for (var y = 0; y < ah; y++)
        {
            for (var x = 0; x < aw; x++)
                mask[y, ax + x] = artMask[y, x];
        }
        for (var y = 0; y < lh; y++)
        {
            for (var x = 0; x < lw; x++)
                mask[top + y, lx + x] = true;
        }
        return new Sprite(index, dim, mask, new Point(ax, 0), new Point(lx, top), lw);
    }

    private static IEnumerable<(float X, float Y)> Spiral(float cx, float cy, float maxR)
    {
        yield return (cx, cy);
        for (var r = (float)Step; r <= maxR; r += Step)
        {
            var count = Math.Max(8, (int)(2 * Math.PI * r / Step));
            for (var i = 0; i < count; i++)
            {
                var a = 2 * Math.PI * i / count;
                yield return (cx + r * MathF.Cos((float)a), cy + r * MathF.Sin((float)a));
            }
        }
    }

    private static List<(int Row, bool[] Bits)> Probes(bool[,] mask)
    {
        var h = mask.GetLength(0);
        var w = mask.GetLength(1);
        var probes = new List<(int, bool[])>();
        var bands = 3;
        for (var b = 0; b < bands; b++)
        {
            var y0 = h * b / bands;
            var y1 = h * (b + 1) / bands;
            var best = y0;
            var bestSum = -1;
            for (var y = y0; y < y1; y++)
            {
                var sum = 0;
                for (var x = 0; x < w; x++)
                {
                    if (mask[y, x])
                        sum++;
                }
                if (sum > bestSum)
                {
                    bestSum = sum;
                    best = y;
                }
            }
            if (bestSum > 0)
            {
                var bits = new bool[w];
                for (var x = 0; x < w; x++)
                    bits[x] = mask[best, x];
                probes.Add((best, bits));
            }
        }
        return probes;
    }

    private static bool RowCollides(bool[,] occ, int y, int x, int w, bool[] bits)
    {
        if (y < 0 || y >= occ.GetLength(0))
            return true;
        for (var i = 0; i < w; i++)
        {
            if (bits[i] && occ[y, x + i])
                return true;
        }
        return false;
    }

    private static bool Collides(bool[,] occ, bool[,] mask, int x, int y)
    {
        var h = mask.GetLength(0);
        var w = mask.GetLength(1);
        for (var row = 0; row < h; row++)
        {
            for (var col = 0; col < w; col++)
            {
                if (mask[row, col] && occ[y + row, x + col])
                    return true;
            }
        }
        return false;
    }

    private static void StampMask(bool[,] occ, bool[,] mask, int x, int y)
    {
        var h = mask.GetLength(0);
        var w = mask.GetLength(1);
        for (var row = 0; row < h; row++)
        {
            for (var col = 0; col < w; col++)
            {
                if (mask[row, col])
                    occ[y + row, x + col] = true;
            }
        }
    }

    private static bool[,] Footprint(Image<L8> alpha)
    {
        var h = alpha.Height;
        var w = alpha.Width;
        var raw = new bool[h, w];
        alpha.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    raw[y, x] = row[x].PackedValue > AlphaCutoff;
            }
        });
        var mask = new bool[h, w];
        var r = OverlapPx;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (!raw[y, x])
                    continue;
                var clear = true;
                for (var yy = Math.Max(0, y - r); yy <= Math.Min(h - 1, y + r) && clear; yy++)
                {
                    for (var xx = Math.Max(0, x - r); xx <= Math.Min(w - 1, x + r); xx++)
                    {
                        if (!raw[yy, xx])
                        {
                            clear = false;
                            break;
                        }
                    }
                }
                mask[y, x] = clear;
            }
        }
        return mask;
    }

    private static Image<L8> Alpha(Image<Rgba32> img)
    {
        var a = new Image<L8>(img.Width, img.Height);
        a.ProcessPixelRows(img, (destAcc, srcAcc) =>
        {
            for (var y = 0; y < destAcc.Height; y++)
            {
                var dest = destAcc.GetRowSpan(y);
                var src = srcAcc.GetRowSpan(y);
                for (var x = 0; x < dest.Length; x++)
                    dest[x] = new L8(src[x].A);
            }
        });
        return a;
    }

    private static Image<Rgba32> Scaled(Image<Rgba32> img, int maxDim, bool flip)
    {
        var scale = maxDim / (float)Math.Max(img.Width, img.Height);
        var w = Math.Max(1, (int)Math.Round(img.Width * scale));
        var h = Math.Max(1, (int)Math.Round(img.Height * scale));
        var copy = img.Clone(c => c.Resize(w, h, KnownResamplers.Lanczos3));
        if (flip)
            copy.Mutate(c => c.Flip(FlipMode.Horizontal));
        return copy;
    }

    private static Image<L8> ScaledGray(Image<L8> img, int maxDim, bool flip)
    {
        var scale = maxDim / (float)Math.Max(img.Width, img.Height);
        var w = Math.Max(1, (int)Math.Round(img.Width * scale));
        var h = Math.Max(1, (int)Math.Round(img.Height * scale));
        var copy = img.Clone(c => c.Resize(w, h, KnownResamplers.Lanczos3));
        if (flip)
            copy.Mutate(c => c.Flip(FlipMode.Horizontal));
        return copy;
    }

    private static List<float> SizeWeights(List<string> names)
    {
        var masses = names.Select(Sizes.MassOf).ToList();
        var geo = MathF.Exp(masses.Sum(MathF.Log) / masses.Count);
        return masses.Select(m => MathF.Pow(m / geo, Sizes.Exponent)).ToList();
    }

    private static bool Flip(string name) => SHA256.HashData(Encoding.UTF8.GetBytes(name))[0] < 128;

    private static Point At(Point at, float scale) =>
        new((int)Math.Round(at.X * scale), (int)Math.Round(at.Y * scale));
}
