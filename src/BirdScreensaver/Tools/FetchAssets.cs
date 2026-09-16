using System.IO.Compression;
using System.Net.Http;

namespace BirdScreensaver.Tools;

internal static class FetchAssets
{
    public const string ZipUrl = "https://github.com/arnegiacomo/fugleramme/archive/refs/heads/main.zip";

    public static async Task RunAsync(IProgress<string>? progress, CancellationToken token)
    {
        var dest = Path.Combine(Paths.AppData, "assets");
        var vendor = Path.Combine(Paths.AppData, "vendor");
        Directory.CreateDirectory(vendor);
        var zip = Path.Combine(vendor, "fugleramme-main.zip");

        progress?.Report("Downloading Fugleramme plates…");
        await DownloadFile(ZipUrl, zip, progress, token);

        progress?.Report("Unpacking plates…");
        ExtractAssets(zip, dest);

        var sln = Path.Combine(Paths.RepoRoot, "BirdScreensaver.sln");
        if (File.Exists(sln))
            CopyTree(dest, Path.Combine(Paths.RepoRoot, "assets"));

        TryDelete(zip);
        progress?.Report("Plates ready");
    }

    private static async Task DownloadFile(string url, string dest, IProgress<string>? progress, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var tmp = dest + ".part";
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("BirdScreensaver/0.1");
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(token);
        await using var output = File.Create(tmp);
        var buffer = new byte[81_920];
        long read = 0;
        int n;
        while ((n = await input.ReadAsync(buffer, token)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, n), token);
            read += n;
            if (total > 0)
                progress?.Report($"Downloading plates ({read * 100 / total}%)");
        }
        output.Close();
        File.Move(tmp, dest, overwrite: true);
    }

    private static void ExtractAssets(string zip, string dest)
    {
        using var archive = ZipFile.OpenRead(zip);
        var root = Path.GetFullPath(dest);
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);

        foreach (var entry in archive.Entries)
        {
            if (!TryAssetsRelative(entry.FullName, out var relative))
                continue;

            var target = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(target, root, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }

        if (!Directory.Exists(Path.Combine(root, "artwork")))
            throw new DirectoryNotFoundException("The Fugleramme archive has no assets folder.");
    }

    internal static bool TryAssetsRelative(string entryName, out string relative)
    {
        relative = "";
        var path = entryName.Replace('\\', '/').TrimStart('/');
        const string marker = "/assets/";
        var at = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
            return false;
        relative = path[(at + marker.Length)..];
        return relative.Length > 0;
    }

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
