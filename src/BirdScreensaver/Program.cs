using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using BirdScreensaver.Detection;
using BirdScreensaver.Settings;
using BirdScreensaver.Tools;
using BirdScreensaver.Windows;

namespace BirdScreensaver;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var parsed = Args.Parse(args);
        var settings = AppSettings.Load();

        try
        {
            return Run(parsed, settings);
        }
        catch (Exception ex)
        {
            EnsureConsole();
            Console.Error.WriteLine(ex);
            if (parsed.Mode is Mode.Configure or Mode.Screensaver or Mode.Window or Mode.PreviewHwnd)
                MessageBox.Show(ex.ToString(), "Bird screensaver");
            return 1;
        }
    }

    private static int Run(Args parsed, AppSettings settings)
    {
        switch (parsed.Mode)
        {
            case Mode.PreviewStill:
                EnsureConsole();
                return PreviewCommand.Run(parsed.Output!, settings, parsed.Demo);
            case Mode.FetchAssets:
                EnsureConsole();
                FetchAssets.Run(new Progress<string>(Console.WriteLine));
                return 0;
            case Mode.FetchModel:
                EnsureConsole();
                ModelDownloader.DownloadAsync(settings.Region, new Progress<string>(Console.WriteLine), CancellationToken.None)
                    .GetAwaiter().GetResult();
                return 0;
            case Mode.Install:
                EnsureConsole();
                Console.WriteLine(Installer.Install());
                return 0;
            case Mode.PreviewHwnd:
                return RunApp(app => PreviewHost.Show(parsed.PreviewParent, settings));
            case Mode.Configure:
                return RunApp(app =>
                {
                    var window = new SettingsWindow(settings);
                    app.MainWindow = window;
                    window.Show();
                });
            case Mode.Window:
            case Mode.Screensaver:
                return RunApp(app =>
                {
                    var window = new ScreensaverWindow(settings, parsed.Mode == Mode.Screensaver, parsed.Demo);
                    app.MainWindow = window;
                    window.Show();
                });
            default:
                return RunApp(app =>
                {
                    var window = new SettingsWindow(settings);
                    app.MainWindow = window;
                    window.Show();
                });
        }
    }

    private static int RunApp(Action<App> start)
    {
        var app = new App();
        start(app);
        return app.Run();
    }

    private static void EnsureConsole()
    {
        if (!AttachConsole(-1))
            AllocConsole();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}

internal enum Mode
{
    Configure,
    Screensaver,
    Window,
    PreviewHwnd,
    PreviewStill,
    FetchAssets,
    FetchModel,
    Install,
}

internal sealed class Args
{
    public Mode Mode { get; init; } = Mode.Configure;
    public bool Demo { get; init; }
    public string? Output { get; init; }
    public nint PreviewParent { get; init; }

    public static Args Parse(string[] args)
    {
        var demo = false;
        var mode = Mode.Configure;
        string? output = null;
        nint preview = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var raw = args[i].Trim();
            var key = raw.TrimStart('/', '-').ToLowerInvariant();
            string? value = null;
            var colon = key.IndexOf(':');
            if (colon >= 0)
            {
                value = key[(colon + 1)..];
                key = key[..colon];
            }

            switch (key)
            {
                case "s":
                    mode = Mode.Screensaver;
                    break;
                case "c":
                case "configure":
                    mode = Mode.Configure;
                    break;
                case "p":
                    mode = Mode.PreviewHwnd;
                    value ??= i + 1 < args.Length ? args[++i] : null;
                    if (nint.TryParse(value, out var hwnd))
                        preview = hwnd;
                    break;
                case "window":
                    mode = Mode.Window;
                    break;
                case "demo":
                    demo = true;
                    break;
                case "preview":
                    mode = Mode.PreviewStill;
                    output = i + 1 < args.Length ? args[++i] : "preview.png";
                    break;
                case "fetch-assets":
                    mode = Mode.FetchAssets;
                    break;
                case "fetch-model":
                    mode = Mode.FetchModel;
                    break;
                case "install":
                    mode = Mode.Install;
                    break;
            }
        }

        return new Args { Mode = mode, Demo = demo, Output = output, PreviewParent = preview };
    }
}
