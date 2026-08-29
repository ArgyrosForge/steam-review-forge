using System.Text.RegularExpressions;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static partial class SteamBbCodeAnalyzer
{
    private static readonly HashSet<string> SupportedTags =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "h1", "h2", "h3", "b", "u", "i", "strike", "spoiler",
            "noparse", "hr", "url", "list", "olist", "quote", "code",
            "table", "tr", "th", "td", "*"
        };

    private static readonly HashSet<string> SpecialContentTags =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "noparse", "code"
        };

    public static BbCodeAnalysisResult Analyze(string? bbCode)
    {
        var result = new BbCodeAnalysisResult();

        if (string.IsNullOrEmpty(bbCode))
        {
            return result;
        }

        var stack = new List<OpenTag>();
        var lines = bbCode
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            AnalyzeLine(lines[lineIndex], lineIndex + 1, stack, result);
        }

        foreach (var openTag in stack.AsEnumerable().Reverse())
        {
            Add(
                result,
                openTag.Line,
                openTag.Column,
                $"[{openTag.Name}] is not closed.");
        }

        return result;
    }

    private static void AnalyzeLine(
        string line,
        int lineNumber,
        List<OpenTag> stack,
        BbCodeAnalysisResult result)
    {
        foreach (Match match in TagPattern().Matches(line))
        {
            var isClosing = match.Groups["closing"].Success;
            var name = match.Groups["name"].Value.ToLowerInvariant();
            var attributes = match.Groups["attributes"].Value;
            var column = match.Index + 1;

            if (stack.Count > 0 &&
                SpecialContentTags.Contains(stack[^1].Name) &&
                !(isClosing &&
                  string.Equals(stack[^1].Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!SupportedTags.Contains(name))
            {
                if (!attributes.StartsWith(' '))
                {
                    Add(result, lineNumber, column, $"[{name}] is not supported by Steam reviews.");
                }

                continue;
            }

            if (name == "*")
            {
                if (isClosing)
                {
                    Add(result, lineNumber, column, "List items use [*] and do not have a closing tag.");
                }
                else if (!stack.Any(tag => tag.Name is "list" or "olist"))
                {
                    Add(result, lineNumber, column, "[*] must appear inside [list] or [olist].");
                }

                continue;
            }

            if (isClosing)
            {
                CloseTag(name, lineNumber, column, stack, result);
                continue;
            }

            ValidateOpeningTag(name, attributes, lineNumber, column, stack, result);
            stack.Add(new OpenTag(name, lineNumber, column));
        }
    }

    private static void ValidateOpeningTag(
        string name,
        string attributes,
        int line,
        int column,
        IReadOnlyList<OpenTag> stack,
        BbCodeAnalysisResult result)
    {
        var parent = stack.Count == 0 ? null : stack[^1].Name;

        if (name == "tr" && parent != "table")
        {
            Add(result, line, column, "[tr] must be directly inside [table].");
        }
        else if (name is "th" or "td" && parent != "tr")
        {
            Add(result, line, column, $"[{name}] must be directly inside [tr].");
        }

        if (name == "table" &&
            attributes.Length > 0 &&
            !attributes.Equals(" noborder=1", StringComparison.OrdinalIgnoreCase) &&
            !attributes.Equals(" equalcells=1", StringComparison.OrdinalIgnoreCase))
        {
            Add(result, line, column, "[table] only supports noborder=1 or equalcells=1.");
        }

        if (name == "url")
        {
            var target = attributes.StartsWith('=')
                ? attributes[1..].Trim()
                : string.Empty;

            if (!IsSafeLinkTarget(target))
            {
                Add(result, line, column, "[url] needs an HTTP or HTTPS target.");
            }
        }
        else if (attributes.StartsWith('=') && name != "quote")
        {
            Add(result, line, column, $"[{name}] does not support a value.");
        }
    }

    private static void CloseTag(
        string name,
        int line,
        int column,
        List<OpenTag> stack,
        BbCodeAnalysisResult result)
    {
        if (stack.Count == 0)
        {
            Add(result, line, column, $"[/{name}] has no matching opening tag.");
            return;
        }

        if (string.Equals(stack[^1].Name, name, StringComparison.OrdinalIgnoreCase))
        {
            stack.RemoveAt(stack.Count - 1);
            return;
        }

        var matchingIndex = stack.FindLastIndex(
            tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase));

        if (matchingIndex < 0)
        {
            Add(result, line, column, $"[/{name}] has no matching opening tag.");
            return;
        }

        Add(
            result,
            line,
            column,
            $"[/{name}] closes before [{stack[^1].Name}] is closed.");

        stack.RemoveAt(matchingIndex);
    }

    private static bool IsSafeLinkTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var candidate = target.Contains("://", StringComparison.Ordinal)
            ? target
            : $"https://{target}";

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
               uri.Scheme is "http" or "https" &&
               !string.IsNullOrWhiteSpace(uri.Host);
    }

    private static void Add(
        BbCodeAnalysisResult result,
        int line,
        int column,
        string message)
    {
        result.Diagnostics.Add(new BbCodeDiagnostic(line, column, message));
    }

    [GeneratedRegex(
        @"\[(?<closing>/)?(?<name>\*|[A-Za-z][A-Za-z0-9]*)(?<attributes>(?:=| )[^\]]*)?\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    private sealed record OpenTag(string Name, int Line, int Column);
}
