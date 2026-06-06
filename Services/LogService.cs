namespace MhxyNotify.Services;

public sealed class LogService
{
    private readonly object _lock = new();

    public string LogPath { get; } = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

    public void Info(string message, int maxLines)
    {
        Write("INFO", message, null, maxLines);
    }

    public void Warn(string message, int maxLines)
    {
        Write("WARN", message, null, maxLines);
    }

    public void Error(string message, Exception? exception, int maxLines)
    {
        Write("ERROR", message, exception, maxLines);
    }

    public string ReadAll()
    {
        lock (_lock)
        {
            return File.Exists(LogPath) ? File.ReadAllText(LogPath) : "";
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.WriteAllText(LogPath, "");
        }
    }

    private void Write(string level, string message, Exception? exception, int maxLines)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            File.AppendAllText(LogPath, line + Environment.NewLine);
            Trim(maxLines);
        }
    }

    private void Trim(int maxLines)
    {
        maxLines = Math.Clamp(maxLines, 100, 50000);
        var lines = File.ReadAllLines(LogPath);
        if (lines.Length <= maxLines)
        {
            return;
        }

        File.WriteAllLines(LogPath, lines.Skip(lines.Length - maxLines));
    }
}
