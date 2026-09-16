namespace BirdScreensaver;

internal static class Paths
{
    public static string AppData
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BirdScreensaver");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile => Path.Combine(AppData, "settings.json");
    public static string PicksFile => Path.Combine(AppData, "artwork.json");
    public static string ModelsDir
    {
        get
        {
            var dir = Path.Combine(AppData, "models");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "BirdScreensaver.sln"))
                    || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return AppContext.BaseDirectory;
        }
    }

    public static string Assets
    {
        get
        {
            foreach (var candidate in new[]
            {
                Path.Combine(AppContext.BaseDirectory, "assets"),
                Path.Combine(AppData, "assets"),
                Path.Combine(RepoRoot, "assets"),
            })
            {
                if (Directory.Exists(Path.Combine(candidate, "artwork")))
                    return candidate;
            }
            return Path.Combine(AppData, "assets");
        }
    }

    public static string Artwork => Path.Combine(Assets, "artwork");
    public static string Fonts => Path.Combine(Assets, "fonts");
    public static string SizesCsv => Path.Combine(Assets, "bird_sizes.csv");
    public static string AliasesJson => Path.Combine(Assets, "birdnet_aliases.json");
    public static string LabelsFile => Path.Combine(Assets, "birdnet_labels_v2.4.txt");
}
