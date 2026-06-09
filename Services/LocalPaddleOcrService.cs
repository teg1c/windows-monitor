using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using System.Drawing.Imaging;

namespace MhxyNotify.Services;

public sealed class LocalPaddleOcrService : IDisposable
{
    private const int MaxRunsBeforeRecycle = 30;
    private const long MaxPrivateMemoryBeforeRecycle = 1_200L * 1024 * 1024;

    private readonly object _lock = new();
    private PaddleOcrAll? _ocr;
    private int _runCount;
    private volatile bool _disposed;

    public Task<OcrReadResult> ReadLatestMessageAsync(Bitmap bitmap)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                using var mat = BitmapToMat(bitmap);
                var result = GetEngine().Run(mat, 1);
                var text = result.Text;
                _runCount++;
                if (_runCount >= MaxRunsBeforeRecycle || IsPrivateMemoryTooHigh())
                {
                    RecycleEngine();
                }

                return OcrTextParser.FromPlainText(text);
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
            AllowRotateDetection = false,
            Enable180Classification = false
        };
        return _ocr;
    }

    private static void ConfigureCpu(PaddleConfig config)
    {
        config.OneDnnEnabled = false;
        config.MkldnnCacheCapacity = 0;
        config.MemoryOptimized = true;
        config.CpuMathThreadCount = Math.Clamp(Environment.ProcessorCount / 4, 1, 2);
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var pixelFormat = bitmap.PixelFormat;
        var clone = pixelFormat is PixelFormat.Format24bppRgb or PixelFormat.Format32bppArgb or PixelFormat.Format32bppPArgb or PixelFormat.Format32bppRgb
            ? null
            : bitmap.Clone(rectangle, PixelFormat.Format24bppRgb);
        var source = clone ?? bitmap;
        BitmapData? data = null;
        try
        {
            data = source.LockBits(rectangle, ImageLockMode.ReadOnly, source.PixelFormat);
            var channels = source.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
            using var sourceMat = Mat.FromPixelData(source.Height, source.Width, channels == 3 ? MatType.CV_8UC3 : MatType.CV_8UC4, data.Scan0, data.Stride);
            if (channels == 3)
            {
                return sourceMat.Clone();
            }

            var bgr = new Mat();
            Cv2.CvtColor(sourceMat, bgr, ColorConversionCodes.BGRA2BGR);
            return bgr;
        }
        finally
        {
            if (data is not null)
            {
                source.UnlockBits(data);
            }

            clone?.Dispose();
        }
    }

    private void RecycleEngine()
    {
        _ocr?.Dispose();
        _ocr = null;
        _runCount = 0;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static bool IsPrivateMemoryTooHigh()
    {
        try
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return process.PrivateMemorySize64 >= MaxPrivateMemoryBeforeRecycle;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        if (!Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(150)))
        {
            return;
        }

        try
        {
            RecycleEngine();
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
