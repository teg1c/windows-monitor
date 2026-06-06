using System.Text;
using MhxyNotify.Models;

namespace MhxyNotify.Services;

public sealed class NotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly HttpClient _httpClient = new();
    private readonly LogService? _logService;

    public NotificationService(LogService? logService = null)
    {
        _logService = logService;
        _notifyIcon = new NotifyIcon
        {
            Icon = AppInfo.LoadApplicationIcon(),
            Text = AppInfo.DisplayName,
            Visible = true
        };
    }

    public async Task NotifyAsync(string title, string body, AppConfig config, NotificationEvent notificationEvent)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = body.Length > 180 ? body[..180] : body;
        _notifyIcon.ShowBalloonTip(5000);

        var channels = config.GetEffectiveNotificationChannels()
            .Where(channel => channel.Enabled && !string.IsNullOrWhiteSpace(channel.Url))
            .ToList();
        if (channels.Count == 0)
        {
            return;
        }

        foreach (var channel in channels)
        {
            try
            {
                await SendWebhookAsync(title, body, channel, notificationEvent);
                _logService?.Info($"通知渠道发送成功：{channel.Name}", config.MaxLogLines);
            }
            catch (Exception ex)
            {
                _logService?.Error($"通知渠道发送失败：{channel.Name}", ex, config.MaxLogLines);
            }
        }
    }

    private async Task SendWebhookAsync(string title, string body, NotificationChannelConfig channel, NotificationEvent notificationEvent)
    {
        var method = new HttpMethod(string.IsNullOrWhiteSpace(channel.Method) ? "POST" : channel.Method.Trim().ToUpperInvariant());
        var url = RenderTemplate(channel.Url, title, body, notificationEvent);
        var request = new HttpRequestMessage(method, url);
        var headers = ParseHeaders(channel.Headers);

        var contentType = "application/json";
        foreach (var (key, value) in headers)
        {
            if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentType = value;
            }
            else
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (method != HttpMethod.Get && !string.IsNullOrWhiteSpace(channel.BodyTemplate))
        {
            var content = RenderTemplate(channel.BodyTemplate, title, body, notificationEvent);
            request.Content = new StringContent(content, Encoding.UTF8, contentType);
        }

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static IReadOnlyList<(string Key, string Value)> ParseHeaders(string headerText)
    {
        return headerText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => (parts[0].Trim(), parts[1].Trim()))
            .ToList();
    }

    private static string RenderTemplate(string template, string title, string body, NotificationEvent notificationEvent)
    {
        return template
            .Replace("{titleRaw}", title, StringComparison.OrdinalIgnoreCase)
            .Replace("{bodyRaw}", body, StringComparison.OrdinalIgnoreCase)
            .Replace("{messageRaw}", body, StringComparison.OrdinalIgnoreCase)
            .Replace("{windowRaw}", notificationEvent.WindowTitle, StringComparison.OrdinalIgnoreCase)
            .Replace("{ocrTextRaw}", notificationEvent.OcrText, StringComparison.OrdinalIgnoreCase)
            .Replace("{titleUrl}", Uri.EscapeDataString(title), StringComparison.OrdinalIgnoreCase)
            .Replace("{bodyUrl}", Uri.EscapeDataString(body), StringComparison.OrdinalIgnoreCase)
            .Replace("{messageUrl}", Uri.EscapeDataString(body), StringComparison.OrdinalIgnoreCase)
            .Replace("{windowUrl}", Uri.EscapeDataString(notificationEvent.WindowTitle), StringComparison.OrdinalIgnoreCase)
            .Replace("{ocrTextUrl}", Uri.EscapeDataString(notificationEvent.OcrText), StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", JsonEscape(title), StringComparison.OrdinalIgnoreCase)
            .Replace("{body}", JsonEscape(body), StringComparison.OrdinalIgnoreCase)
            .Replace("{message}", JsonEscape(body), StringComparison.OrdinalIgnoreCase)
            .Replace("{window}", JsonEscape(notificationEvent.WindowTitle), StringComparison.OrdinalIgnoreCase)
            .Replace("{distance}", notificationEvent.Distance.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{region}", JsonEscape(notificationEvent.RegionText), StringComparison.OrdinalIgnoreCase)
            .Replace("{ocrText}", JsonEscape(notificationEvent.OcrText), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", JsonEscape(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")), StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string JsonEscape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
        _httpClient.Dispose();
    }
}

public sealed record NotificationEvent(string WindowTitle, int Distance, Rectangle Region, string OcrText)
{
    public string RegionText => $"x={Region.X},y={Region.Y},w={Region.Width},h={Region.Height}";
}
