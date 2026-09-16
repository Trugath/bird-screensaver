using SixLabors.Fonts;
using Path = System.IO.Path;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BirdScreensaver.Render;

internal static class Page
{
    public static readonly Color Ink = Color.FromRgb(30, 30, 30);
    private const float LineSpacing = 0.1f;
    private const float PerchFill = 0.7f;

    public static Image<Rgba32> Trim(string path)
    {
        using var img = Image.Load<Rgba32>(path);
        var minX = img.Width;
        var minY = img.Height;
        var maxX = 0;
        var maxY = 0;
        img.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A == 0)
                        continue;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        });
        if (maxX < minX)
            return img.Clone();
        return img.Clone(c => c.Crop(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)));
    }

    public static Image<Rgba32> Fit(Image<Rgba32> img, int boxW, int boxH)
    {
        var scale = Math.Min(boxW / (float)img.Width, boxH / (float)img.Height);
        var w = Math.Max(1, (int)Math.Round(img.Width * scale));
        var h = Math.Max(1, (int)Math.Round(img.Height * scale));
        return img.Clone(c => c.Resize(w, h, KnownResamplers.Lanczos3));
    }

    public static Image<L8> TextMask(string text, Font font)
    {
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            LineSpacing = 1 + LineSpacing,
        };
        var size = TextMeasurer.MeasureSize(text, options);
        var w = Math.Max(1, (int)Math.Ceiling(size.Width) + 2);
        var h = Math.Max(1, (int)Math.Ceiling(size.Height) + 2);
        var mask = new Image<L8>(w, h, new L8(0));
        options.Origin = new PointF(w / 2f, 1);
        mask.Mutate(c => c.DrawText(options, text, Color.White));
        return mask;
    }

    public static void Stamp(Image<Rgb24> canvas, Image<L8> mask, int x, int y)
    {
        canvas.ProcessPixelRows(mask, (destAcc, maskAcc) =>
        {
            for (var row = 0; row < maskAcc.Height; row++)
            {
                var cy = y + row;
                if (cy < 0 || cy >= destAcc.Height)
                    continue;
                var dest = destAcc.GetRowSpan(cy);
                var src = maskAcc.GetRowSpan(row);
                for (var col = 0; col < src.Length; col++)
                {
                    var cx = x + col;
                    if (cx < 0 || cx >= dest.Length || src[col].PackedValue < 16)
                        continue;
                    var t = src[col].PackedValue / 255f;
                    dest[cx] = new Rgb24(
                        (byte)(dest[cx].R * (1 - t) + 30 * t),
                        (byte)(dest[cx].G * (1 - t) + 30 * t),
                        (byte)(dest[cx].B * (1 - t) + 30 * t));
                }
            }
        });
    }

    public static Image<Rgb24> Blank(int width, int height, bool textured) =>
        textured ? Paper.Texture(width, height) : new Image<Rgb24>(width, height, Paper.Target);

    public static void DrawPerch(Image<Rgb24> canvas, IReadOnlyList<string> perches, int day)
    {
        if (perches.Count == 0)
        {
            DrawFallbackPerch(canvas);
            return;
        }
        using var perch = Trim(perches[day % perches.Count]);
        if ((day / perches.Count) % 2 == 1)
            perch.Mutate(c => c.Flip(FlipMode.Horizontal));
        var target = (int)(Math.Min(canvas.Width, canvas.Height) * PerchFill);
        using var fitted = Fit(perch, target, target);
        using var proc = Paper.ProcessSprite(fitted);
        var x = (canvas.Width - proc.Width) / 2;
        var y = (canvas.Height - proc.Height) / 2;
        canvas.Mutate(c => c.DrawImage(proc, new Point(x, y), 1f));
    }

    public static void DrawFallbackPerch(Image<Rgb24> canvas)
    {
        var w = canvas.Width;
        var h = canvas.Height;
        var y = (int)(h * 0.62);
        var x0 = (int)(w * 0.18);
        var x1 = (int)(w * 0.82);
        var ink = Color.FromRgb(92, 74, 55);
        canvas.Mutate(c =>
        {
            c.DrawLine(ink, Math.Max(4, h / 180f), new PointF(x0, y + 18), new PointF((x0 + x1) / 2f, y), new PointF(x1, y + 12));
            c.DrawLine(ink, Math.Max(2, h / 260f), new PointF((x0 + x1) / 2f, y), new PointF((x0 + x1) / 2f + 40, y - 36));
        });
    }
}
