using System.Text.Json;

namespace MhxyNotify.Models;

public sealed class AppConfig
{
    public int PollIntervalMs { get; set; } = 300;
    public int Threshold { get; set; } = 36;
    public int StableSamples { get; set; } = 2;
    public int CooldownSeconds { get; set; } = 10;
    public bool NotifyOnFirstSeen { get; set; }
    public string MonitorMode { get; set; } = "region";
    public string WindowTitle { get; set; } = "";
    public string WindowClassName { get; set; } = "";
    public RectDto Region { get; set; } = new();
    public bool WebhookEnabled { get; set; }
    public string WebhookPreset { get; set; } = "generic";
    public string WebhookMethod { get; set; } = "POST";
    public string WebhookUrl { get; set; } = "";
    public string WebhookHeaders { get; set; } = "Content-Type: application/json";
    public string WebhookBodyTemplate { get; set; } = "{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{time}\",\"window\":\"{window}\",\"distance\":{distance}}";
    public List<NotificationChannelConfig> NotificationChannels { get; set; } = [];
    public bool OcrEnabled { get; set; } = true;
    public string OcrMode { get; set; } = "local";
    public string OcrUrl { get; set; } = "http://192.168.88.3:5000/ocr";
    public string OcrCommand { get; set; } = "tesseract";
    public string OcrArguments { get; set; } = "\"{image}\" stdout -l chi_sim+eng --psm 6";
    public double MinConfidence { get; set; } = 0.45;
    public int MaxOcrPixels { get; set; } = 800_000;
    public bool FullWindowKeywordEnabled { get; set; }
    public string KeywordDetectionMode { get; set; } = "ocr";
    public int KeywordOcrIntervalMs { get; set; } = 2000;
    public string WatchKeywords { get; set; } = "\u7f51\u7edc\u9519\u8bef\r\n\u8bf7\u91cd\u65b0\u767b\u5f55\r\n\u7f51\u7edc\u6709\u95ee\u9898";
    public int MaxLogLines { get; set; } = 1000;

    public static string DefaultPath => AppPaths.ConfigPath;

    public static AppConfig Load()
    {
        AppPaths.CopyLegacyFileIfNeeded(AppPaths.LegacyConfigPath, DefaultPath);
        if (!File.Exists(DefaultPath))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(DefaultPath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions()) ?? new AppConfig();
    }

    public void Save()
    {
        AppPaths.EnsureDataDirectory();
        var json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(DefaultPath, json);
    }

    public IReadOnlyList<NotificationChannelConfig> GetEffectiveNotificationChannels()
    {
        NotificationChannels ??= [];
        if (NotificationChannels.Count > 0)
        {
            return NotificationChannels;
        }

        if (!WebhookEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
        {
            return [];
        }

        return
        [
            new NotificationChannelConfig
            {
                Name = string.IsNullOrWhiteSpace(WebhookPreset) ? "Webhook" : WebhookPreset,
                Enabled = WebhookEnabled,
                Preset = WebhookPreset,
                Method = WebhookMethod,
                Url = WebhookUrl,
                Headers = WebhookHeaders,
                BodyTemplate = WebhookBodyTemplate
            }
        ];
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}

public sealed class NotificationChannelConfig
{
    public string Name { get; set; } = "Webhook";
    public bool Enabled { get; set; }
    public string Preset { get; set; } = "generic";
    public string Method { get; set; } = "POST";
    public string Url { get; set; } = "";
    public string Headers { get; set; } = "Content-Type: application/json";
    public string BodyTemplate { get; set; } = "{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{time}\",\"window\":\"{window}\",\"distance\":{distance}}";

    public NotificationChannelConfig Clone()
    {
        return new NotificationChannelConfig
        {
            Name = Name,
            Enabled = Enabled,
            Preset = Preset,
            Method = Method,
            Url = Url,
            Headers = Headers,
            BodyTemplate = BodyTemplate
        };
    }
}

public sealed class RectDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Rectangle ToRectangle() => new(X, Y, Width, Height);

    public static RectDto FromRectangle(Rectangle rectangle)
    {
        return new RectDto
        {
            X = rectangle.X,
            Y = rectangle.Y,
            Width = rectangle.Width,
            Height = rectangle.Height
        };
    }
}
