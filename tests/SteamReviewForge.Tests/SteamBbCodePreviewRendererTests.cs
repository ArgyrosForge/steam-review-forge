using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class SteamBbCodePreviewRendererTests
{
    [Fact]
    public void Render_SupportsMultilineCodeNoParseAndAttributedQuote()
    {
        const string bbCode = """
            [code]
            first  line
            <script>alert('x')</script>
            [/code]
            [noparse]
            [b]literal[/b]
            next line
            [/noparse]
            [quote=Author]
            First line
            Second [b]line[/b]
            [/quote]
            """;

        var html = SteamBbCodePreviewRenderer.Render(bbCode);

        Assert.Contains("<pre class=\"preview-code\"><code>", html);
        Assert.Contains("&lt;script&gt;alert", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("[b]literal[/b]<br />", html);
        Assert.Contains("Originally posted by", html);
        Assert.Contains("<strong>Author</strong>", html);
        Assert.Contains("Second <strong>line</strong>", html);
    }

    [Fact]
    public void Render_DoesNotCreateUnsafeLinks()
    {
        var html = SteamBbCodePreviewRenderer.Render(
            "[url=javascript:alert(1)]Unsafe[/url]");

        Assert.DoesNotContain("href=", html);
        Assert.Contains("Unsafe", html);
    }

    [Fact]
    public void Render_DistinguishesEqualAndAutomaticTableWidths()
    {
        var equal = SteamBbCodePreviewRenderer.Render(
            "[table equalcells=1]\n[tr][td]A[/td][/tr]\n[/table]");
        var automatic = SteamBbCodePreviewRenderer.Render(
            "[table]\n[tr][td]A[/td][/tr]\n[/table]");

        Assert.Contains("preview-table-equal", equal);
        Assert.DoesNotContain("preview-table-equal", automatic);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("quote")]
    [InlineData("noparse")]
    public void Render_PreservesTextAroundSameLineAndMultilineBlocks(string tag)
    {
        foreach (var content in new[] { "inside", "first\nsecond" })
        {
            var html = SteamBbCodePreviewRenderer.Render($"before [{tag}]{content}[/{tag}] after [{tag}]last[/{tag}] tail");
            Assert.Contains("before", html);
            Assert.Contains("after", html);
            Assert.Contains("last", html);
            Assert.Contains("tail", html);
        }
    }

    [Fact]
    public void Render_KeepsMultilineEmphasisInsideOneParagraph()
    {
        var html = SteamBbCodePreviewRenderer.Render("[b]one\ntwo[/b]");
        Assert.Contains("<p><strong>one<br />two</strong></p>", html);
    }

    [Fact]
    public void Render_SupportsSteamDocumentedMultilineTablesAndInlineLists()
    {
        var html = SteamBbCodePreviewRenderer.Render("""
            [table equalcells=1]
                [tr]
                    [th]Aspect[/th]
                    [th]Notes[/th]
                [/tr]
                [tr]
                    [td]Controls[/td]
                    [td][u]Precise[/u] [spoiler]secret[/spoiler][/td]
                [/tr]
            [/table]
            [list][*]First[*]Second[/list]
            [olist][*]Third[/olist]
            """);
        Assert.Contains("<th>Aspect</th>", html);
        Assert.Contains("<u>Precise</u>", html);
        Assert.Contains("preview-spoiler", html);
        Assert.Contains("preview-table-equal", html);
        Assert.Contains("<ul class=\"preview-bbcode-list\">", html);
        Assert.Contains("<ol class=\"preview-bbcode-list\">", html);
        Assert.DoesNotContain("[tr]", html);
        Assert.DoesNotContain("[*]", html);
    }

    [Fact]
    public void Render_SupportsSteamDocumentedSchemeLessLinks()
    {
        var html = SteamBbCodePreviewRenderer.Render("[url=store.steampowered.com]Store[/url]");
        Assert.Contains("href=\"https://store.steampowered.com/\"", html);
        Assert.Contains("noopener noreferrer", html);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///tmp/test")]
    public void Render_RejectsNonWebLinkSchemes(string target)
    {
        var html = SteamBbCodePreviewRenderer.Render($"[url={target}]label[/url]");
        Assert.DoesNotContain("href=", html);
        Assert.Contains("label", html);
    }

    [Fact]
    public void Render_EncodesTextAndQuoteAttributes()
    {
        var html = SteamBbCodePreviewRenderer.Render("[quote=<img src=x onerror=alert(1)>]<script>alert(1)</script>[/quote]");
        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("<script", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_BoundsOversizeAndDeepInputs()
    {
        Assert.Contains(SteamBbCodeSyntax.SizeMessage,
            SteamBbCodePreviewRenderer.Render(new string('x', SteamBbCodeSyntax.MaximumCharacters + 1)));
        var nested = string.Concat(Enumerable.Repeat("[b]", 10000));
        Assert.Contains("nested too deeply", SteamBbCodePreviewRenderer.Render(nested));
    }

    [Fact]
    public void Render_MalformedInputsCompleteWithinAGenerousBudget()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        foreach (var token in new[] { "[url=https://example.com]", "[code]\n", "[" })
        {
            var input = string.Concat(Enumerable.Repeat(token, 40000 / token.Length));
            Assert.NotEmpty(SteamBbCodePreviewRenderer.Render(input));
        }
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), $"Malformed rendering took {timer.Elapsed}.");
    }

    [Fact]
    public void Render_AndAnalyzerAgreeAtTheNestingLimit()
    {
        var source = string.Concat(Enumerable.Repeat("[b]", SteamBbCodeSyntax.MaximumNesting)) + "text" +
            string.Concat(Enumerable.Repeat("[/b]", SteamBbCodeSyntax.MaximumNesting));
        Assert.Empty(SteamBbCodeAnalyzer.Analyze(source).Diagnostics);
        Assert.DoesNotContain("nested too deeply", SteamBbCodePreviewRenderer.Render(source));
    }
}
