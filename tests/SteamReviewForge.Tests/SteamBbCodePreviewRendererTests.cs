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
}
