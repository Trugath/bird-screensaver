using Microsoft.Win32;

namespace BirdScreensaver.Tools;

internal static class Installer
{
    public static string InstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BirdScreensaver");

    public static string Install()
    {
        var dest = Normalize(InstallDir);
        Directory.CreateDirectory(dest);

        var source = Normalize(AppContext.BaseDirectory);
        var processDir = Normalize(Path.GetDirectoryName(Environment.ProcessPath) ?? source);
        if (!Same(source, dest) && !Same(processDir, dest))
            CopyTree(source, dest);

        var exe = FirstExisting(
            Path.Combine(dest, "BirdScreensaver.exe"),
            Path.Combine(processDir, "BirdScreensaver.exe"),
            Path.ChangeExtension(Environment.ProcessPath, ".exe"));
        if (exe is null)
            throw new FileNotFoundException("BirdScreensaver.exe was not in the build output.");

        var scr = Path.ChangeExtension(exe, ".scr");
        CopyFile(exe, scr);

        var registered = File.Exists(scr) ? scr : Environment.ProcessPath ?? exe;
        RegisterScreensaver(registered);
        return registered;
    }

    public static void RegisterScreensaver(string scr)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true)
            ?? throw new InvalidOperationException("Could not open the desktop settings key.");
        key.SetValue("SCRNSAVE.EXE", scr);
        key.SetValue("ScreenSaveActive", "1");
    }

    private static void CopyTree(string from, string to)
    {
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(from, file);
            CopyFile(file, Path.Combine(to, relative));
        }
    }

    private static void CopyFile(string from, string to)
    {
        if (Same(from, to) || string.IsNullOrWhiteSpace(from) || !File.Exists(from))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(to))!);
        try
        {
            File.Copy(from, to, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings and the Windows preview keep this tree loaded.
        }
    }

    private static string? FirstExisting(params string?[] paths)
    {
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;
        }

        return null;
    }

    private static bool Same(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
