using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;

namespace MhxyNotify.Services;

public sealed class LocalPaddleOcrService : IDisposable
{
    private readonly object _lock = new();
    private PaddleOcrAll? _ocr;

    public Task<OcrReadResult> ReadLatestMessageAsync(Bitmap bitmap)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                using var mat = BitmapToMat(bitmap);
                var result = GetEngine().Run(mat, 4);
                return OcrTextParser.FromPlainText(result.Text);
            }
        });
    }

    private PaddleOcrAll GetEngine()
    {
        if (_ocr is not null)
        {
            return _ocr;
        }

        _ocr = new PaddleOcrAll(LocalFullModels.ChineseV5, ConfigureCpu)
        {
            AllowRotateDetection = true,
            Enable180Classification = false
        };
        return _ocr;
    }

    private static void ConfigureCpu(PaddleConfig config)
    {
        config.OneDnnEnabled = true;
        config.MkldnnCacheCapacity = 10;
        config.CpuMathThreadCount = Math.Max(1, Environment.ProcessorCount / 2);
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return Cv2.ImDecode(stream.ToArray(), ImreadModes.Color);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _ocr?.Dispose();
            _ocr = null;
        }
    }
}
