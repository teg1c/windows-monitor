namespace MhxyNotify;

public static class AppPaths
{
    private const string AppDirectoryName = "windows-monitor";

    public static string DataDirectory
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return string.IsNullOrWhiteSpace(localAppData)
                ? AppContext.BaseDirectory
                : Path.Combine(localAppData, AppDirectoryName);
        }
    }

    public static string ConfigPath => Path.Combine(DataDirectory, "config.local.json");

    public static string StatePath => Path.Combine(DataDirectory, ".windows-monitor-state.json");

    public static string LogPath => Path.Combine(DataDirectory, "logs", "app.log");

    public static string LegacyConfigPath => Path.Combine(AppContext.BaseDirectory, "config.local.json");

    public static string LegacyStatePath => Path.Combine(AppContext.BaseDirectory, ".mhxy-notify-state.json");

    public static string LegacyLogPath => Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

    public static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    public static void CopyLegacyFileIfNeeded(string legacyPath, string currentPath)
    {
        if (Path.GetFullPath(legacyPath).Equals(Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase) ||
            File.Exists(currentPath) ||
            !File.Exists(legacyPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(currentPath)!);
        File.Copy(legacyPath, currentPath, overwrite: false);
    }
}
