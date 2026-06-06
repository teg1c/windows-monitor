using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MhxyNotify.Native;

public sealed record WindowInfo(IntPtr Handle, string Title, string ClassName, Rectangle? CaptureBounds = null)
{
    public bool IsDesktopSource => Handle == IntPtr.Zero &&
        (string.Equals(ClassName, "DESKTOP", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(ClassName, "MONITOR", StringComparison.OrdinalIgnoreCase));

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(ClassName) ? Title : $"{Title}  [{ClassName}]";
    }
}

public static class Win32Window
{
    private const int Srccopy = 0x00CC0020;
    private const int SmXvirtualscreen = 76;
    private const int SmYvirtualscreen = 77;
    private const int SmCxvirtualscreen = 78;
    private const int SmCyvirtualscreen = 79;

    public static WindowInfo DesktopWindow { get; } = new(IntPtr.Zero, "\u6574\u4e2a\u684c\u9762", "DESKTOP");

    public static IReadOnlyList<WindowInfo> ListVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd))
            {
                return true;
            }

            var title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            windows.Add(new WindowInfo(hwnd, title, GetWindowClassName(hwnd)));
            return true;
        }, IntPtr.Zero);

        return GetDesktopSources()
            .Concat(windows.OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase))
            .ToList();
    }

    public static Size GetCaptureSize(WindowInfo window)
    {
        return window.IsDesktopSource ? GetDesktopBounds(window).Size : GetClientSize(window.Handle);
    }

    public static Size GetClientSize(IntPtr hwnd)
    {
        if (!GetClientRect(hwnd, out var rect))
        {
            throw new InvalidOperationException("\u65e0\u6cd5\u8bfb\u53d6\u7a97\u53e3\u5ba2\u6237\u7aef\u5927\u5c0f\u3002");
        }

        return new Size(Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
    }

    public static Bitmap Capture(WindowInfo window)
    {
        return window.IsDesktopSource ? CaptureDesktop(GetDesktopBounds(window)) : CaptureClient(window.Handle);
    }

    public static Bitmap CaptureClient(IntPtr hwnd)
    {
        var size = GetClientSize(hwnd);
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new InvalidOperationException("\u7a97\u53e3\u5ba2\u6237\u7aef\u533a\u57df\u4e3a\u7a7a\u3002");
        }

        return CaptureClientRegion(hwnd, new Rectangle(0, 0, size.Width, size.Height));
    }

    public static Bitmap CaptureClientRegion(IntPtr hwnd, Rectangle region)
    {
        var size = GetClientSize(hwnd);
        region = Rectangle.Intersect(region, new Rectangle(Point.Empty, size));
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new InvalidOperationException("\u76d1\u63a7\u533a\u57df\u4e3a\u7a7a\u6216\u8d85\u51fa\u7a97\u53e3\u8303\u56f4\u3002");
        }

        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        var sourceDc = GetDC(hwnd);
        if (sourceDc == IntPtr.Zero)
        {
            bitmap.Dispose();
            throw new InvalidOperationException("\u65e0\u6cd5\u83b7\u53d6\u7a97\u53e3 DC\u3002");
        }

        var targetDc = graphics.GetHdc();
        try
        {
            if (!BitBlt(targetDc, 0, 0, region.Width, region.Height, sourceDc, region.X, region.Y, Srccopy))
            {
                bitmap.Dispose();
                throw new InvalidOperationException("\u7a97\u53e3\u622a\u56fe\u5931\u8d25\u3002");
            }
        }
        finally
        {
            graphics.ReleaseHdc(targetDc);
            ReleaseDC(hwnd, sourceDc);
        }

        return bitmap;
    }

    private static IReadOnlyList<WindowInfo> GetDesktopSources()
    {
        var sources = new List<WindowInfo> { DesktopWindow with { CaptureBounds = GetVirtualScreenBounds() } };
        var index = 1;
        foreach (var screen in Screen.AllScreens)
        {
            var title = screen.Primary
                ? $"\u663e\u793a\u5668 {index}\uff08\u4e3b\u5c4f\uff09"
                : $"\u663e\u793a\u5668 {index}";
            sources.Add(new WindowInfo(IntPtr.Zero, title, "MONITOR", screen.Bounds));
            index++;
        }

        return sources;
    }

    private static Rectangle GetDesktopBounds(WindowInfo window)
    {
        return window.CaptureBounds ?? GetVirtualScreenBounds();
    }

    private static Bitmap CaptureDesktop(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("\u684c\u9762\u533a\u57df\u4e3a\u7a7a\u3002");
        }

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        var desktopDc = GetDC(IntPtr.Zero);
        if (desktopDc == IntPtr.Zero)
        {
            bitmap.Dispose();
            throw new InvalidOperationException("\u65e0\u6cd5\u83b7\u53d6\u684c\u9762 DC\u3002");
        }

        var targetDc = graphics.GetHdc();
        try
        {
            if (!BitBlt(targetDc, 0, 0, bounds.Width, bounds.Height, desktopDc, bounds.X, bounds.Y, Srccopy))
            {
                bitmap.Dispose();
                throw new InvalidOperationException("\u684c\u9762\u622a\u56fe\u5931\u8d25\u3002");
            }
        }
        finally
        {
            graphics.ReleaseHdc(targetDc);
            ReleaseDC(IntPtr.Zero, desktopDc);
        }

        return bitmap;
    }

    private static Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            GetSystemMetrics(SmXvirtualscreen),
            GetSystemMetrics(SmYvirtualscreen),
            GetSystemMetrics(SmCxvirtualscreen),
            GetSystemMetrics(SmCyvirtualscreen));
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return "";
        }

        var buffer = new char[length + 1];
        GetWindowText(hwnd, buffer, buffer.Length);
        return new string(buffer).TrimEnd('\0');
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        var buffer = new char[256];
        GetClassName(hwnd, buffer, buffer.Length);
        return new string(buffer).TrimEnd('\0');
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, char[] text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, char[] className, int count);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
