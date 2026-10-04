using SteamReviewForge.Models;
using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class SteamFormattingCompatibilityTests
{
    [Fact]
    public void ReferenceSample_HasNoCompatibilityWarningsAndRendersAllSupportedFeatures()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "steam-formatting-sample.txt"));
        Assert.Empty(SteamBbCodeAnalyzer.Analyze(source).Diagnostics);
        var html = SteamBbCodePreviewRenderer.Render(source);
        foreach (var marker in new[] { "<h1>", "<h2>", "<h3>", "<strong>", "<em>", "<u>", "<s>",
                     "preview-spoiler", "<hr", "<a href=", "preview-noparse", "preview-code",
                     "<blockquote", "<ul", "<ol", "<li>", "<table", "<tr>", "<th>", "<td>",
                     "preview-table-equal", "preview-table-borderless", "preview-embed" })
            Assert.Contains(marker, html);
        Assert.Contains("Text after the code block.", html);
        Assert.Contains("Text after the quote.", html);
        Assert.Contains("Text after the literal block.", html);
        Assert.Contains("[b]This stays literal[/b]", html);
    }

    [Fact]
    public void AllTemplateLayoutAndRatingCombinations_GenerateSupportedSteamMarkup()
    {
        foreach (var template in Enum.GetValues<ReviewTemplate>())
        foreach (var format in Enum.GetValues<ReviewDisplayFormat>())
        foreach (var rating in Enum.GetValues<ReviewRatingSystem>())
        {
            var draft = new ReviewDraft();
            ReviewTemplateService.Apply(draft, template);
            draft.DisplayFormat = format;
            draft.RatingSystem = rating;
            var source = SteamBbCodeGenerator.Generate(draft);
            var issues = SteamBbCodeAnalyzer.Analyze(source);
            Assert.True(!issues.HasWarnings, $"{template}/{format}/{rating}: {string.Join(", ", issues.Diagnostics.Select(d => d.Message))}");
            var html = SteamBbCodePreviewRenderer.Render(source);
            Assert.DoesNotContain("[table", html);
            Assert.DoesNotContain("[tr]", html);
            Assert.DoesNotContain("[h1]", html);
        }
    }
}
