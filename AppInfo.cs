namespace MhxyNotify;

public static class AppInfo
{
    public const string EnglishName = "windows-monitor";
    public const string DisplayName = "\u7075\u8baf\u54e8";
    public const string NotificationTitle = "\u7075\u8baf\u54e8\u63d0\u9192";
    public const string MutexName = @"Local\windows-monitor.SingleInstance";
    public const string RepositoryOwner = "teg1c";
    public const string RepositoryName = "windows-monitor";
    public const string LatestReleaseApiUrl = "https://api.github.com/repos/teg1c/windows-monitor/releases/latest";

    public static string Version => typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    public static string FullTitle => $"\u7075\u8baf\u54e8 - \u7cfb\u7edf\u6d88\u606f\u76d1\u63a7 v{Version}";

    public static Icon LoadApplicationIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "windows-monitor.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var embedded = Icon.ExtractAssociatedIcon(processPath);
            if (embedded is not null)
            {
                return embedded;
            }
        }

        return SystemIcons.Application;
    }
}
