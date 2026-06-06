using System.Diagnostics;
using System.Text.Json;

namespace MhxyNotify.Services;

public sealed class UpdateService : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly LogService _logService;

    public UpdateService(LogService logService)
    {
        _logService = logService;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.EnglishName}/{AppInfo.Version}");
    }

    public async Task<UpdateInfo?> CheckLatestAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(AppInfo.LatestReleaseApiUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logService.Warn($"检查更新失败：GitHub 返回 {(int)response.StatusCode}", 1000);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var tag = root.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() ?? "" : "";
        var htmlUrl = root.TryGetProperty("html_url", out var htmlElement) ? htmlElement.GetString() ?? "" : "";
        var installerUrl = FindAssetUrl(root, "windows-monitor-setup.exe");
        if (string.IsNullOrWhiteSpace(installerUrl))
        {
            installerUrl = FindAssetUrl(root, "windows-monitor.zip");
        }

        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(installerUrl))
        {
            return null;
        }

        return new UpdateInfo(NormalizeVersion(tag), htmlUrl, installerUrl);
    }

    public static bool IsNewer(UpdateInfo updateInfo)
    {
        return Version.TryParse(NormalizeVersion(updateInfo.Version), out var latest) &&
               Version.TryParse(NormalizeVersion(AppInfo.Version), out var current) &&
               latest > current;
    }

    public async Task<string> DownloadInstallerAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default)
    {
        var extension = updateInfo.DownloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ".zip" : ".exe";
        var path = Path.Combine(Path.GetTempPath(), $"{AppInfo.EnglishName}-update-{updateInfo.Version}{extension}");
        await using var input = await _httpClient.GetStreamAsync(updateInfo.DownloadUrl, cancellationToken);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, cancellationToken);
        return path;
    }

    public static void LaunchInstaller(string path)
    {
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            {
                UseShellExecute = true
            });
            return;
        }

        Process.Start(new ProcessStartInfo(path, "--quiet")
        {
            UseShellExecute = true
        });
    }

    private static string FindAssetUrl(JsonElement release, string assetName)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "" : "";
            if (!string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() ?? "" : "";
        }

        return "";
    }

    private static string NormalizeVersion(string value)
    {
        return value.Trim().TrimStart('v', 'V');
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public sealed record UpdateInfo(string Version, string PageUrl, string DownloadUrl);
