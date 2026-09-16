using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using BirdScreensaver.Artwork;
using BirdScreensaver.Render;
using BirdScreensaver.Settings;

namespace BirdScreensaver.Windows;

internal static class PreviewHost
{
    public static void Show(IntPtr parent, AppSettings settings)
    {
        if (!GetClientRect(parent, out var rect))
            return;
        var width = Math.Max(16, rect.Right - rect.Left);
        var height = Math.Max(16, rect.Bottom - rect.Top);

        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Width = width,
            Height = height,
            Background = System.Windows.Media.Brushes.Transparent,
        };
        var image = new System.Windows.Controls.Image { Stretch = System.Windows.Media.Stretch.UniformToFill };
        window.Content = image;

        var catalog = new Catalog(Paths.Artwork, new Picks(Paths.PicksFile));
        catalog.UseStyle(settings.Style);
        using var page = Collage.Render(
            [],
            new SixLabors.ImageSharp.Size(Math.Max(width, 160), Math.Max(height, 100)),
            settings.ShowNames,
            settings.Font,
            settings.LabelSize,
            catalog.Perches);
        image.Source = BitmapConvert.ToBitmap(page);

        window.SourceInitialized += (_, _) =>
        {
            var helper = new WindowInteropHelper(window);
            SetParent(helper.Handle, parent);
            SetWindowPos(helper.Handle, IntPtr.Zero, 0, 0, width, height, 0x0040);
        };
        window.Show();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}
