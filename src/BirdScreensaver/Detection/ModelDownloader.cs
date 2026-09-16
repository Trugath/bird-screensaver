using System.Net.Http;

namespace BirdScreensaver.Detection;

internal static class ModelDownloader
{
    public static async Task DownloadAsync(string region, IProgress<string>? progress, CancellationToken token)
    {
        var model = ModelCatalog.LocalModel(region);
        var labels = ModelCatalog.LocalLabels(region);
        await DownloadFile(ModelCatalog.ModelUrl(region), model, progress, token);
        await DownloadFile(ModelCatalog.LabelsUrl(region), labels, progress, token);
        progress?.Report("Model ready");
    }

    private static async Task DownloadFile(string url, string dest, IProgress<string>? progress, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var tmp = dest + ".part";
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("BirdScreensaver/0.1");
        using var response = await http.GetAsync(url + "?download=true", HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(token);
        await using var output = File.Create(tmp);
        var buffer = new byte[82_192];
        long read = 0;
        int n;
        while ((n = await input.ReadAsync(buffer, token)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, n), token);
            read += n;
            if (total > 0)
                progress?.Report($"Downloading {Path.GetFileName(dest)} ({read * 100 / total}%)");
        }
        output.Close();
        File.Move(tmp, dest, overwrite: true);
    }
}
