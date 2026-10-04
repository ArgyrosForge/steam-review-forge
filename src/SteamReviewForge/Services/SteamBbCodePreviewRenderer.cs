using System.Net;
using System.Text;

namespace SteamReviewForge.Services;

public static class SteamBbCodePreviewRenderer
{
    private static readonly HashSet<string> Tags =
    ["h1", "h2", "h3", "b", "i", "u", "strike", "spoiler", "url", "code",
     "noparse", "quote", "hr", "list", "olist", "*", "table", "tr", "th", "td"];

    public static string Render(string bbCode)
    {
        if (string.IsNullOrWhiteSpace(bbCode))
            return "<p class=\"preview-empty\">Start writing to see a preview.</p>";
        if (bbCode.Length > SteamBbCodeSyntax.MaximumCharacters)
            return $"<p class=\"preview-empty\">{SteamBbCodeSyntax.SizeMessage}</p>";

        var text = bbCode.Replace("\r\n", "\n").Replace('\r', '\n');
        var root = Parse(text);
        if (root is null)
            return "<p class=\"preview-empty\">Formatting is nested too deeply. Showing literal BBCode.</p>" +
                   $"<pre class=\"preview-code\">{Encode(text)}</pre>";
        return RenderFlow(root.Children);
    }

    private static Node? Parse(string text)
    {
        var root = new Node("root");
        var stack = new List<Node> { root };
        var position = 0;
        foreach (var token in SteamBbCodeSyntax.Scan(text))
        {
            if (token.Start < position) continue; // Already consumed a literal block.
            AddText(stack[^1], text[position..token.Start]);
            position = token.End;
            if (!Tags.Contains(token.Name))
            {
                AddText(stack[^1], text[token.Start..token.End]);
                continue;
            }
            if (token.Closing)
            {
                if (token.Name == "hr") continue;
                var index = stack.FindLastIndex(node => node.Tag == token.Name);
                if (index > 0) stack.RemoveRange(index, stack.Count - index);
                else AddText(stack[^1], text[token.Start..token.End]);
                continue;
            }
            if (token.Name is "code" or "noparse")
            {
                var closing = $"[/{token.Name}]";
                var end = text.IndexOf(closing, position, StringComparison.OrdinalIgnoreCase);
                if (end < 0)
                {
                    AddText(stack[^1], text[token.Start..]);
                    position = text.Length;
                    break;
                }
                stack[^1].Children.Add(new Node(token.Name) { Text = text[position..end] });
                position = end + closing.Length;
                continue;
            }
            if (token.Name == "*")
            {
                var list = stack.FindLastIndex(node => node.Tag is "list" or "olist");
                if (list < 0)
                {
                    AddText(stack[^1], "[*]");
                    continue;
                }
                stack.RemoveRange(list + 1, stack.Count - list - 1);
            }
            var node = new Node(token.Name, token.Attributes);
            stack[^1].Children.Add(node);
            if (token.Name == "hr") continue;
            if (stack.Count > SteamBbCodeSyntax.MaximumNesting) return null;
            stack.Add(node);
        }
        AddText(stack[^1], text[position..]);
        return root;
    }

    private static void AddText(Node parent, string text)
    {
        if (text.Length > 0) parent.Children.Add(new Node("text") { Text = text });
    }

    private static bool IsBlock(Node node) => node.Tag is
        "h1" or "h2" or "h3" or "quote" or "code" or "hr" or "list" or "olist" or "table";

    private static string RenderFlow(IEnumerable<Node> children, bool insideLink = false)
    {
        var output = new StringBuilder();
        var paragraph = new StringBuilder();
        void Flush()
        {
            var content = paragraph.ToString().Trim();
            while (content.EndsWith("<br />", StringComparison.Ordinal)) content = content[..^6].TrimEnd();
            if (content.Length > 0) output.Append("<p>").Append(content).AppendLine("</p>");
            paragraph.Clear();
        }
        foreach (var child in children)
        {
            if (IsBlock(child))
            {
                Flush();
                output.Append(RenderNode(child, insideLink));
            }
            else if (child.Tag == "text")
            {
                // Treat newlines as paragraph boundaries only outside inline tags.
                var lines = child.Text.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    if (i > 0) Flush();
                    if (paragraph.Length == 0 && TryRenderEmbed(lines[i].Trim(), out var embed))
                        output.Append(embed);
                    else paragraph.Append(Encode(lines[i]));
                }
            }
            else paragraph.Append(RenderNode(child, insideLink));
        }
        Flush();
        return output.ToString();
    }

    private static string RenderInline(IEnumerable<Node> children, bool insideLink = false) =>
        string.Concat(children.Select(child => RenderNode(child, insideLink)));

    private static string RenderNode(Node node, bool insideLink)
    {
        string Inline() => RenderInline(node.Children, insideLink);
        string Flow() => RenderFlow(node.Children, insideLink);
        switch (node.Tag)
        {
            case "text": return Encode(node.Text).Replace("\n", "<br />", StringComparison.Ordinal);
            case "code": return $"<pre class=\"preview-code\"><code>{Encode(node.Text)}</code></pre>";
            case "noparse": return $"<span class=\"preview-noparse\">{Encode(node.Text).Replace("\n", "<br />", StringComparison.Ordinal)}</span>";
            case "hr": return "<hr />";
            case "b": return $"<strong>{Inline()}</strong>";
            case "i": return $"<em>{Inline()}</em>";
            case "u": return $"<u>{Inline()}</u>";
            case "strike": return $"<s>{Inline()}</s>";
            case "spoiler": return $"<span class=\"preview-spoiler\" tabindex=\"0\">{Inline()}</span>";
            case "h1": case "h2": case "h3": return $"<{node.Tag}>{Inline()}</{node.Tag}>";
            case "quote":
                var author = node.Attributes.StartsWith('=') ? node.Attributes[1..] : string.Empty;
                return string.IsNullOrWhiteSpace(author)
                    ? $"<blockquote>{Flow()}</blockquote>"
                    : "<blockquote class=\"preview-attributed-quote\"><span class=\"preview-quote-author\">Originally posted by " +
                      $"<strong>{Encode(author)}</strong>:</span>{Flow()}</blockquote>";
            case "url":
                var target = node.Attributes.StartsWith('=') ? node.Attributes[1..] : string.Empty;
                if (insideLink || !SteamBbCodeSyntax.TryGetLink(target, out var uri)) return Inline();
                return $"<a href=\"{Encode(uri!.AbsoluteUri)}\" target=\"_blank\" rel=\"noopener noreferrer\">" +
                       RenderInline(node.Children, true) + "</a>";
            case "list": case "olist":
                var listTag = node.Tag == "list" ? "ul" : "ol";
                return $"<{listTag} class=\"preview-bbcode-list\">" +
                       string.Concat(node.Children.Where(child => child.Tag != "text" || !string.IsNullOrWhiteSpace(child.Text))
                           .Select(child => child.Tag == "*" ? $"<li>{RenderFlow(child.Children, insideLink)}</li>" : $"<li>{RenderNode(child, insideLink)}</li>")) +
                       $"</{listTag}>";
            case "table":
                var tableClass = node.Attributes.Trim().ToLowerInvariant() switch
                {
                    "noborder=1" => "preview-table preview-table-borderless",
                    "equalcells=1" => "preview-table preview-table-equal",
                    _ => "preview-table"
                };
                return $"<div class=\"preview-table-wrapper\"><table class=\"{tableClass}\">" +
                       string.Concat(node.Children.Where(child => child.Tag != "text" || !string.IsNullOrWhiteSpace(child.Text))
                           .Select(child => RenderNode(child, insideLink))) + "</table></div>";
            case "tr":
                return "<tr>" + string.Concat(node.Children.Where(child => child.Tag != "text" || !string.IsNullOrWhiteSpace(child.Text))
                    .Select(child => RenderNode(child, insideLink))) + "</tr>";
            case "th": case "td": return $"<{node.Tag}>{Inline().Trim()}</{node.Tag}>";
            default: return Inline();
        }
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static bool TryRenderEmbed(string line, out string embed)
    {
        embed = string.Empty;
        if (!Uri.TryCreate(line, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return false;
        var host = uri.Host.ToLowerInvariant();
        var label = host is "youtube.com" or "www.youtube.com" or "youtu.be"
            ? "YouTube video"
            : host == "store.steampowered.com" ? "Steam Store page"
            : host is "steamcommunity.com" or "www.steamcommunity.com" && uri.AbsolutePath.StartsWith("/sharedfiles/", StringComparison.OrdinalIgnoreCase)
                ? "Steam Community item" : string.Empty;
        if (label.Length == 0) return false;
        var url = Encode(uri.AbsoluteUri);
        embed = $"<a class=\"preview-embed\" href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\"><strong>{label}</strong><span>{url}</span></a>";
        return true;
    }

    private sealed class Node(string tag, string attributes = "")
    {
        public string Tag { get; } = tag;
        public string Attributes { get; } = attributes;
        public string Text { get; init; } = string.Empty;
        public List<Node> Children { get; } = [];
    }
}
