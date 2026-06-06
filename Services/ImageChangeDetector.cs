using System.Security.Cryptography;
using System.Numerics;

namespace MhxyNotify.Services;

public sealed class ImageChangeDetector
{
    private string? _lastHash;
    private string? _pendingHash;
    private int _pendingCount;
    private DateTimeOffset _lastNotifiedAt = DateTimeOffset.MinValue;

    public ChangeResult Check(Bitmap bitmap, int threshold, int stableSamples, int cooldownSeconds)
    {
        var hash = AverageHash(bitmap, 64, 32);
        if (_lastHash is null)
        {
            _lastHash = hash;
            return new ChangeResult(true, false, 0, hash);
        }

        var distance = HammingDistance(_lastHash, hash);
        if (distance < threshold)
        {
            _pendingHash = null;
            _pendingCount = 0;
            return new ChangeResult(false, false, distance, hash);
        }

        if (_pendingHash == hash)
        {
            _pendingCount++;
        }
        else
        {
            _pendingHash = hash;
            _pendingCount = 1;
        }

        if (_pendingCount < Math.Max(1, stableSamples))
        {
            return new ChangeResult(false, false, distance, hash);
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, cooldownSeconds));
        if (cooldown > TimeSpan.Zero && DateTimeOffset.Now - _lastNotifiedAt < cooldown)
        {
            _lastHash = hash;
            _pendingHash = null;
            _pendingCount = 0;
            return new ChangeResult(false, false, distance, hash);
        }

        _lastHash = hash;
        _pendingHash = null;
        _pendingCount = 0;
        _lastNotifiedAt = DateTimeOffset.Now;
        return new ChangeResult(false, true, distance, hash);
    }

    private static string AverageHash(Bitmap bitmap, int width, int height)
    {
        using var scaled = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            graphics.DrawImage(bitmap, 0, 0, width, height);
        }

        var values = new byte[width * height];
        var sum = 0;
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = scaled.GetPixel(x, y);
                var gray = (byte)((color.R * 299 + color.G * 587 + color.B * 114) / 1000);
                values[index++] = gray;
                sum += gray;
            }
        }

        var average = sum / values.Length;
        var bytes = new byte[(values.Length + 7) / 8];
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] >= average)
            {
                bytes[i / 8] |= (byte)(1 << (7 - i % 8));
            }
        }

        return Convert.ToHexString(bytes);
    }

    private static int HammingDistance(string left, string right)
    {
        var leftBytes = Convert.FromHexString(left);
        var rightBytes = Convert.FromHexString(right);
        var length = Math.Min(leftBytes.Length, rightBytes.Length);
        var distance = Math.Abs(leftBytes.Length - rightBytes.Length) * 8;
        for (var i = 0; i < length; i++)
        {
            distance += BitOperations.PopCount((uint)(leftBytes[i] ^ rightBytes[i]));
        }

        return distance;
    }
}

public sealed record ChangeResult(bool FirstSeen, bool Changed, int Distance, string Hash);
