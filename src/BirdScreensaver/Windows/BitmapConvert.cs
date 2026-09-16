using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Bmp;

namespace BirdScreensaver.Windows;

internal static class BitmapConvert
{
    public static BitmapImage ToBitmap(Image<Rgb24> image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new BmpEncoder());
        ms.Position = 0;
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = ms;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}
