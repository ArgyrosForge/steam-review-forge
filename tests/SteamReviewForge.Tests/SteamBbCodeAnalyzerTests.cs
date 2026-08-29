using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class SteamBbCodeAnalyzerTests
{
    [Fact]
    public void Analyze_AcceptsSupportedNestedMarkup()
    {
        const string bbCode = """
            [h1]Review[/h1]
            [b]Bold [i]and italic[/i][/b]
            [list]
            [*]First
            [*]Second
            [/list]
            [table equalcells=1]
            [tr][th]Name[/th][th]Score[/th][/tr]
            [tr][td]Gameplay[/td][td]9/10[/td][/tr]
            [/table]
            [url=https://store.steampowered.com]Store[/url]
            """;

        var result = SteamBbCodeAnalyzer.Analyze(bbCode);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Analyze_ReportsUnsupportedUnclosedAndMisnestedTags()
    {
        const string bbCode = "[b][i]Text[/b]\n[flash]Movie[/flash]";

        var result = SteamBbCodeAnalyzer.Analyze(bbCode);

        Assert.Contains(result.Diagnostics, issue =>
            issue.Message.Contains("closes before", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, issue =>
            issue.Message.Contains("not closed", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, issue =>
            issue.Message.Contains("not supported", StringComparison.Ordinal));
        Assert.All(result.Diagnostics, issue => Assert.True(issue.Line > 0));
    }

    [Fact]
    public void Analyze_IgnoresMarkupInsideNoParseAndCode()
    {
        const string bbCode = """
            [noparse][madeup]literal[/madeup][/noparse]
            [code]
            [also-made-up]
            [/code]
            """;

        var result = SteamBbCodeAnalyzer.Analyze(bbCode);

        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("[*]Outside a list", "inside [list]")]
    [InlineData("[tr][td]Cell[/td][/tr]", "inside [table]")]
    [InlineData("[url=javascript:alert(1)]Click[/url]", "HTTP or HTTPS")]
    [InlineData("[table striped=1][/table]", "noborder=1")]
    public void Analyze_ReportsStructuralCompatibilityProblems(
        string bbCode,
        string expectedMessage)
    {
        var result = SteamBbCodeAnalyzer.Analyze(bbCode);

        Assert.Contains(result.Diagnostics, issue =>
            issue.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }
}
