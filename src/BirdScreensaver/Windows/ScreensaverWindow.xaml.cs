using System.Windows;
using System.Windows.Input;
using Size = SixLabors.ImageSharp.Size;
using BirdScreensaver.Artwork;
using BirdScreensaver.Detection;
using BirdScreensaver.Render;
using BirdScreensaver.Settings;

namespace BirdScreensaver.Windows;

public partial class ScreensaverWindow : Window
{
    private readonly AppSettings _settings;
    private readonly bool _fullscreen;
    private readonly Catalog _catalog;
    private readonly Hearing _hearing = new();
    private readonly IBirdSource _source;
    private System.Windows.Point? _origin;
    private DateTime _started;
    private string _lastKey = "\0";
    private int _busy;

    internal ScreensaverWindow(AppSettings settings, bool fullscreen, bool demo)
    {
        InitializeComponent();
        _settings = settings;
        _fullscreen = fullscreen;
        _catalog = new Catalog(Paths.Artwork, new Picks(Paths.PicksFile));
        _catalog.UseStyle(settings.Style);

        if (fullscreen)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
            Cursor = Cursors.None;
        }
        else
        {
            Width = 1280;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = "Bird screensaver";
        }

        _source = demo || settings.Demo
            ? new DemoSource(_catalog.Drawable, _hearing)
            : new LiveSource(settings, _hearing);
        _source.Status += s => Dispatcher.Invoke(() => StatusText.Text = s);
        _source.Heard += _ => RequestRender();

        Loaded += OnLoaded;
        Closed += (_, _) => _source.Dispose();
        KeyDown += (_, _) => QuitIfFullscreen();
        MouseDown += (_, _) => QuitIfFullscreen();
        MouseMove += OnMouseMove;
        SizeChanged += (_, _) => RequestRender();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _started = DateTime.UtcNow;
        _source.Start();
        RequestRender();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_fullscreen)
            return;
        if ((DateTime.UtcNow - _started).TotalMilliseconds < 400)
            return;
        var here = e.GetPosition(this);
        if (_origin is null)
        {
            _origin = here;
            return;
        }
        if (Math.Abs(here.X - _origin.Value.X) + Math.Abs(here.Y - _origin.Value.Y) > 8)
            Close();
    }

    private void QuitIfFullscreen()
    {
        if (_fullscreen)
            Close();
    }

    private void RequestRender()
    {
        var key = _hearing.Key(_settings.Lookback, _settings.MaxBirds) + "|" + ActualWidth + "x" + ActualHeight;
        if (key == _lastKey || Interlocked.Exchange(ref _busy, 1) == 1)
            return;
        _lastKey = key;
        var names = _hearing.Current(_settings.Lookback, _settings.MaxBirds);
        var width = Math.Max(320, (int)Math.Round(ActualWidth));
        var height = Math.Max(200, (int)Math.Round(ActualHeight));
        if (width < 2 || height < 2)
        {
            Interlocked.Exchange(ref _busy, 0);
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                var entries = _catalog.Entries(names);
                using var page = Collage.Render(
                    entries,
                    new Size(width, height),
                    _settings.ShowNames,
                    _settings.Font,
                    _settings.LabelSize,
                    _catalog.Perches);
                var bmp = BitmapConvert.ToBitmap(page);
                Dispatcher.Invoke(() => PageImage.Source = bmp);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => StatusText.Text = ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        });
    }
}
