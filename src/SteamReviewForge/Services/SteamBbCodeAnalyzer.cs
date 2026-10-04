using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class SteamBbCodeAnalyzer
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

        if (bbCode.Length > SteamBbCodeSyntax.MaximumCharacters)
        {
            Add(result, 1, 1, SteamBbCodeSyntax.SizeMessage);
            result.IsIncomplete = true;
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
            if (result.IsIncomplete) break;
        }

        // If scanning stopped early, later closing tags have not been read.
        // Do not report those still-open tags as definitively unclosed.
        if (result.IsIncomplete) return result;

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
        foreach (var token in SteamBbCodeSyntax.Scan(line))
        {
            if (result.IsIncomplete) return;
            var isClosing = token.Closing;
            var name = token.Name;
            var attributes = token.Attributes;
            var column = token.Start + 1;

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
            if (stack.Count >= SteamBbCodeSyntax.MaximumNesting)
            {
                Add(result, lineNumber, column, "Formatting is nested too deeply; simplify the markup to resume full checks.");
                result.IsIncomplete = true;
                return;
            }
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
        return SteamBbCodeSyntax.TryGetLink(target, out _);
    }

    private static void Add(
        BbCodeAnalysisResult result,
        int line,
        int column,
        string message)
    {
        if (result.Diagnostics.Count < SteamBbCodeSyntax.MaximumDiagnostics)
            result.Diagnostics.Add(new BbCodeDiagnostic(line, column, message));
        else
            result.IsIncomplete = true;
    }

    private sealed record OpenTag(string Name, int Line, int Column);
}
