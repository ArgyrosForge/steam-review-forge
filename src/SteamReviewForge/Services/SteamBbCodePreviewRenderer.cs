using System.Net;
using System.Text;

namespace SteamReviewForge.Services;

public static class SteamBbCodePreviewRenderer
{
    public static string Render(string bbCode)
    {
        if (string.IsNullOrWhiteSpace(bbCode))
        {
            return """
                <p class="preview-empty">
                    Start writing to see a preview.
                </p>
                """;
        }

        var output = new StringBuilder();

        var lines = bbCode
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryGetTagContent(line, "h1", out var headingOne))
            {
                output.AppendLine(
                    $"<h1>{RenderInline(headingOne)}</h1>");

                continue;
            }

            if (TryGetTagContent(line, "h2", out var headingTwo))
            {
                output.AppendLine(
                    $"<h2>{RenderInline(headingTwo)}</h2>");

                continue;
            }

            if (TryGetTagContent(line, "i", out var italicText))
            {
                output.AppendLine(
                    $"<p class=\"preview-summary\">" +
                    $"<em>{RenderInline(italicText)}</em></p>");

                continue;
            }

            if (line.Equals(
                    "[hr][/hr]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("<hr />");
                continue;
            }

            if (line.Equals(
                    "[table equalcells=1]",
                    StringComparison.OrdinalIgnoreCase) ||
                line.Equals(
                    "[table]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine(
                    "<div class=\"preview-table-wrapper\">" +
                    "<table class=\"preview-table\">");

                continue;
            }

            if (line.Equals(
                    "[/table]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("</table></div>");
                continue;
            }

            if (line.StartsWith(
                    "[tr]",
                    StringComparison.OrdinalIgnoreCase) &&
                line.EndsWith(
                    "[/tr]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine(RenderTableRow(line));
                continue;
            }

            if (line.StartsWith(
                    "• ",
                    StringComparison.Ordinal))
            {
                output.AppendLine(
                    $"<p class=\"preview-list-item\">" +
                    $"<span aria-hidden=\"true\">•</span>" +
                    $"<span>{RenderInline(line[2..])}</span>" +
                    $"</p>");

                continue;
            }

            if (line.StartsWith(
                    "☑",
                    StringComparison.Ordinal) ||
                line.StartsWith(
                    "☐",
                    StringComparison.Ordinal))
            {
                var marker = line[..1];
                var text = line[1..].TrimStart();

                output.AppendLine(
                    $"<p class=\"preview-check-item\">" +
                    $"<span class=\"preview-check-marker\" " +
                    $"aria-hidden=\"true\">{marker}</span>" +
                    $"<span>{RenderInline(text)}</span>" +
                    $"</p>");

                continue;
            }

            output.AppendLine(
                $"<p>{RenderInline(line)}</p>");
        }

        return output.ToString();
    }

    private static bool TryGetTagContent(
        string line,
        string tag,
        out string content)
    {
        var openingTag = $"[{tag}]";
        var closingTag = $"[/{tag}]";

        if (line.StartsWith(
                openingTag,
                StringComparison.OrdinalIgnoreCase) &&
            line.EndsWith(
                closingTag,
                StringComparison.OrdinalIgnoreCase))
        {
            content = line[
                openingTag.Length..
                ^closingTag.Length];

            return true;
        }

        content = string.Empty;
        return false;
    }

    private static string RenderTableRow(string line)
    {
        return WebUtility.HtmlEncode(line)
            .Replace(
                "[tr]",
                "<tr>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/tr]",
                "</tr>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[th]",
                "<th>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/th]",
                "</th>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[td]",
                "<td>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/td]",
                "</td>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[b]",
                "<strong>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/b]",
                "</strong>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[i]",
                "<em>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/i]",
                "</em>",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string RenderInline(string text)
    {
        return WebUtility.HtmlEncode(text)
            .Replace(
                "[b]",
                "<strong>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/b]",
                "</strong>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[i]",
                "<em>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/i]",
                "</em>",
                StringComparison.OrdinalIgnoreCase);
    }
}