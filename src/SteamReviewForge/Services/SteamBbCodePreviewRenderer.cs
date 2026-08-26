using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamReviewForge.Services;

public static class SteamBbCodePreviewRenderer
{
    private static readonly Regex UrlPattern =
        new(
            @"\[url=(?<url>[^\]]+)\](?<text>.*?)\[/url\]",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex AttributedQuotePattern =
        new(
            @"^\[quote=(?<author>[^\]]+)\](?<text>.*)\[/quote\]$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

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

            if (TryGetTagContent(line, "h3", out var headingThree))
            {
                output.AppendLine(
                    $"<h3>{RenderInline(headingThree)}</h3>");

                continue;
            }

            if (TryGetTagContent(line, "i", out var italicText))
            {
                output.AppendLine(
                    $"<p class=\"preview-summary\">" +
                    $"<em>{RenderInline(italicText)}</em></p>");

                continue;
            }

            if (TryGetTagContent(line, "noparse", out var unparsedText))
            {
                output.AppendLine(
                    $"<p class=\"preview-noparse\">" +
                    $"{WebUtility.HtmlEncode(unparsedText)}</p>");

                continue;
            }

            if (TryGetTagContent(line, "code", out var codeText))
            {
                output.AppendLine(
                    $"<pre class=\"preview-code\"><code>" +
                    $"{WebUtility.HtmlEncode(codeText)}</code></pre>");

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
                    "[list]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("<ul class=\"preview-bbcode-list\">");
                continue;
            }

            if (line.Equals(
                    "[/list]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("</ul>");
                continue;
            }

            if (line.Equals(
                    "[olist]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("<ol class=\"preview-bbcode-list\">");
                continue;
            }

            if (line.Equals(
                    "[/olist]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("</ol>");
                continue;
            }

            if (line.StartsWith(
                    "[*]",
                    StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine(
                    $"<li>{RenderInline(line[3..].Trim())}</li>");
                continue;
            }

            var attributedQuote = AttributedQuotePattern.Match(line);

            if (attributedQuote.Success)
            {
                output.AppendLine(
                    "<blockquote class=\"preview-attributed-quote\">" +
                    "<span class=\"preview-quote-author\">" +
                    "Originally posted by " +
                    $"<strong>{WebUtility.HtmlEncode(attributedQuote.Groups["author"].Value)}</strong>:" +
                    "</span>" +
                    $"{RenderInline(attributedQuote.Groups["text"].Value)}" +
                    "</blockquote>");

                continue;
            }

            if (TryGetTagContent(line, "quote", out var quote))
            {
                output.AppendLine(
                    $"<blockquote>{RenderInline(quote)}</blockquote>");
                continue;
            }

            if (line.StartsWith(
                    "[table",
                    StringComparison.OrdinalIgnoreCase) &&
                line.EndsWith(
                    "]",
                    StringComparison.Ordinal))
            {
                var tableClass = line.Equals(
                    "[table noborder=1]",
                    StringComparison.OrdinalIgnoreCase)
                    ? "preview-table preview-table-borderless"
                    : line.Equals(
                        "[table equalcells=1]",
                        StringComparison.OrdinalIgnoreCase)
                        ? "preview-table preview-table-equal"
                        : "preview-table";

                output.AppendLine(
                    "<div class=\"preview-table-wrapper\">" +
                    $"<table class=\"{tableClass}\">");

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

            if (TryRenderEmbed(line, out var embed))
            {
                output.AppendLine(embed);
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
            content = line[openingTag.Length..^closingTag.Length];

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
        var encoded = WebUtility.HtmlEncode(text);

        encoded = UrlPattern.Replace(
            encoded,
            match => RenderLink(
                match.Groups["url"].Value,
                match.Groups["text"].Value));

        return encoded
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
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[u]",
                "<u>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/u]",
                "</u>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[strike]",
                "<s>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/strike]",
                "</s>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[spoiler]",
                "<span class=\"preview-spoiler\">",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[/spoiler]",
                "</span>",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryRenderEmbed(
        string line,
        out string embed)
    {
        embed = string.Empty;

        if (!Uri.TryCreate(line, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var label = host is "youtube.com" or "www.youtube.com" or "youtu.be"
            ? "YouTube video"
            : host is "store.steampowered.com"
                ? "Steam Store page"
                : host is "steamcommunity.com" or "www.steamcommunity.com" &&
                  uri.AbsolutePath.StartsWith(
                      "/sharedfiles/",
                      StringComparison.OrdinalIgnoreCase)
                    ? "Steam Community item"
                    : string.Empty;

        if (string.IsNullOrEmpty(label))
        {
            return false;
        }

        var encodedUrl = WebUtility.HtmlEncode(uri.AbsoluteUri);

        embed =
            $"<a class=\"preview-embed\" href=\"{encodedUrl}\" " +
            "target=\"_blank\" rel=\"noopener noreferrer\">" +
            $"<strong>{label}</strong><span>{encodedUrl}</span></a>";

        return true;
    }

    private static string RenderLink(
        string encodedUrl,
        string encodedText)
    {
        var url = WebUtility.HtmlDecode(encodedUrl);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return encodedText;
        }

        return
            $"<a href=\"{encodedUrl}\" target=\"_blank\" " +
            $"rel=\"noopener noreferrer\">{encodedText}</a>";
    }
}
