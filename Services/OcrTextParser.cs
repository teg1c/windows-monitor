using System.Text;
using System.Text.RegularExpressions;

namespace MhxyNotify.Services;

public static class OcrTextParser
{
    public static OcrReadResult FromPlainText(string text)
    {
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeText)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return new OcrReadResult(ExtractLatestSystemMessage(lines), string.Join(Environment.NewLine, lines));
    }

    public static OcrReadResult FromPositionedItems<T>(
        IReadOnlyList<T> items,
        Func<T, string> textSelector,
        Func<T, double> leftSelector,
        Func<T, double> topSelector)
    {
        var rows = new List<List<T>>();
        foreach (var item in items)
        {
            var row = rows.FirstOrDefault(existing => Math.Abs(existing.Average(topSelector) - topSelector(item)) <= 16);
            if (row is null)
            {
                rows.Add([item]);
            }
            else
            {
                row.Add(item);
            }
        }

        var lines = rows
            .OrderBy(row => row.Average(topSelector))
            .Select(row => string.Join("", row.OrderBy(leftSelector).Select(item => textSelector(item).Trim())))
            .Select(NormalizeText)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return new OcrReadResult(ExtractLatestSystemMessage(lines), string.Join(Environment.NewLine, lines));
    }

    public static string NormalizeText(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (!char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString()
            .Replace('\uff1a', ':')
            .Trim();
    }

    private static string ExtractLatestSystemMessage(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return "";
        }

        var selected = new List<string>();
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            selected.Add(lines[i]);
            if (LooksLikeSystemStart(lines[i]))
            {
                selected.Reverse();
                return NormalizeText(string.Join("", selected));
            }
        }

        return lines[^1];
    }

    private static bool LooksLikeSystemStart(string line)
    {
        return line.Contains("\u7cfb\u7edf", StringComparison.Ordinal) ||
               Regex.IsMatch(line, @"^\s*\[?\u7cfb\u7edf\]?");
    }
}
