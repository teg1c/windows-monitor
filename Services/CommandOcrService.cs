using System.Diagnostics;
using System.ComponentModel;
using System.Text;

namespace MhxyNotify.Services;

public sealed class CommandOcrService
{
    public static bool TryResolveCommand(string command, out string resolvedCommand, out string error)
    {
        resolvedCommand = "";
        error = "";

        if (string.IsNullOrWhiteSpace(command))
        {
            error = "\u672c\u5730 OCR \u547d\u4ee4\u4e3a\u7a7a\u3002";
            return false;
        }

        command = command.Trim().Trim('"');
        if (HasDirectoryPart(command))
        {
            if (File.Exists(command))
            {
                resolvedCommand = command;
                return true;
            }

            error = $"\u627e\u4e0d\u5230 OCR \u547d\u4ee4\u6587\u4ef6\uff1a{command}";
            return false;
        }

        foreach (var directory in GetSearchDirectories())
        {
            foreach (var candidate in GetCommandCandidates(directory, command))
            {
                if (File.Exists(candidate))
                {
                    resolvedCommand = candidate;
                    return true;
                }
            }
        }

        error = $"\u627e\u4e0d\u5230 OCR \u547d\u4ee4\u201c{command}\u201d\u3002\u8bf7\u5b89\u88c5\u5b83\u5e76\u52a0\u5165 PATH\uff0c\u6216\u586b\u5199\u5b8c\u6574 exe \u8def\u5f84\uff0c\u6216\u5207\u6362\u5230 wxocr \u6a21\u5f0f\u3002";
        return false;
    }

    public async Task<OcrReadResult> ReadLatestMessageAsync(Bitmap bitmap, string command, string arguments)
    {
        if (!TryResolveCommand(command, out var resolvedCommand, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var imagePath = Path.Combine(Path.GetTempPath(), "mhxy-notify-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            var expandedArguments = arguments.Replace("{image}", imagePath, StringComparison.OrdinalIgnoreCase);
            var startInfo = new ProcessStartInfo
            {
                FileName = resolvedCommand,
                Arguments = expandedArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = StartProcess(startInfo);
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!await WaitForExitAsync(process, TimeSpan.FromSeconds(12)))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("OCR command timed out.");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "OCR command failed." : stderr.Trim());
            }

            return OcrTextParser.FromPlainText(stdout);
        }
        finally
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch
            {
                // Temporary OCR image cleanup is best effort.
            }
        }
    }

    private static Process StartProcess(ProcessStartInfo startInfo)
    {
        try
        {
            return Process.Start(startInfo) ?? throw new InvalidOperationException("OCR command failed to start.");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"\u542f\u52a8 OCR \u547d\u4ee4\u5931\u8d25\uff1a{startInfo.FileName}\u3002{ex.Message}", ex);
        }
    }

    private static IEnumerable<string> GetSearchDirectories()
    {
        yield return AppContext.BaseDirectory;

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return directory.Trim();
        }
    }

    private static IEnumerable<string> GetCommandCandidates(string directory, string command)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            yield break;
        }

        yield return Path.Combine(directory, command);

        if (Path.HasExtension(command))
        {
            yield break;
        }

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        foreach (var extension in pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return Path.Combine(directory, command + extension.Trim());
        }
    }

    private static bool HasDirectoryPart(string command)
    {
        return command.Contains(Path.DirectorySeparatorChar) ||
               command.Contains(Path.AltDirectorySeparatorChar) ||
               Path.IsPathRooted(command);
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
