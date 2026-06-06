using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MhxyNotify.Services;

public sealed class WxOcrService : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public async Task<OcrReadResult> ReadLatestMessageAsync(Bitmap bitmap, string endpoint, double minConfidence)
    {
        await using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        var base64 = Convert.ToBase64String(stream.ToArray());

        using var response = await _httpClient.PostAsJsonAsync(endpoint, new { image = base64 });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WxOcrEnvelope>();
        var payload = result?.Result ?? result?.AsResponse();
        if (payload is null)
        {
            return new OcrReadResult("", "");
        }

        var items = payload.OcrResponse
            .Where(item => item.Rate >= minConfidence && !string.IsNullOrWhiteSpace(item.Text))
            .OrderBy(item => item.Top)
            .ThenBy(item => item.Left)
            .ToList();

        return OcrTextParser.FromPositionedItems(items, item => item.Text, item => item.Left, item => item.Top);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public sealed record OcrReadResult(string LatestMessage, string FullText);

internal sealed class WxOcrResponse
{
    [JsonPropertyName("errcode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("ocr_response")]
    public List<WxOcrItem> OcrResponse { get; set; } = [];
}

internal sealed class WxOcrEnvelope
{
    [JsonPropertyName("result")]
    public WxOcrResponse? Result { get; set; }

    [JsonPropertyName("errcode")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("ocr_response")]
    public List<WxOcrItem>? OcrResponse { get; set; }

    public WxOcrResponse? AsResponse()
    {
        if (OcrResponse is null && ErrorCode is null)
        {
            return null;
        }

        return new WxOcrResponse
        {
            ErrorCode = ErrorCode ?? 0,
            OcrResponse = OcrResponse ?? []
        };
    }
}

internal sealed class WxOcrItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("left")]
    public double Left { get; set; }

    [JsonPropertyName("top")]
    public double Top { get; set; }

    [JsonPropertyName("right")]
    public double Right { get; set; }

    [JsonPropertyName("bottom")]
    public double Bottom { get; set; }

    [JsonPropertyName("rate")]
    public double Rate { get; set; }
}
