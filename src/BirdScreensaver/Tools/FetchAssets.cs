using System.Diagnostics;

namespace BirdScreensaver.Tools;

internal static class FetchAssets
{
    public static void Run(IProgress<string>? progress = null)
    {
        var dest = Path.Combine(Paths.AppData, "assets");
        var vendor = Path.Combine(Paths.AppData, "vendor", "fugleramme");
        Directory.CreateDirectory(Path.GetDirectoryName(vendor)!);
        progress?.Report("Cloning Fugleramme assets…");

        if (!Directory.Exists(Path.Combine(vendor, ".git")))
        {
            RunGit(Paths.AppData, [
                "clone", "--depth", "1", "--filter=blob:none", "--sparse",
                "https://github.com/arnegiacomo/fugleramme.git", vendor,
            ], progress);
        }
        else
        {
            try
            {
                RunGit(vendor, ["pull", "--ff-only"], progress);
            }
            catch (InvalidOperationException ex)
            {
                progress?.Report(ex.Message);
            }
        }
        RunGit(vendor, ["sparse-checkout", "set", "assets"], progress);

        var source = Path.Combine(vendor, "assets");
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("Fugleramme clone has no assets folder.");

        CopyTree(source, dest);
        var repoAssets = Path.Combine(Paths.RepoRoot, "assets");
        if (Directory.Exists(Paths.RepoRoot))
            CopyTree(source, repoAssets);

        progress?.Report("Plates ready");
    }

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(from, dir);
            Directory.CreateDirectory(Path.Combine(to, relative));
        }
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void RunGit(string cwd, string[] args, IProgress<string>? progress)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);
        using var proc = Process.Start(start) ?? throw new InvalidOperationException("Could not start git.");
        var output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException("git failed: " + output);
        if (!string.IsNullOrWhiteSpace(output))
            progress?.Report(output.Trim());
    }
}
