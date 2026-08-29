using SteamReviewForge.Models;
using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class ReviewDraftValidatorTests
{
    [Fact]
    public void Validate_ReportsRequiredStructuredFields()
    {
        var draft = new ReviewDraft
        {
            Summary = string.Empty,
            Recommendation = null,
            Title = string.Empty,
            FinalThoughts = string.Empty,
            Categories = []
        };

        var result = ReviewDraftValidator.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == nameof(draft.Recommendation));
        Assert.Contains(result.Issues, issue => issue.Field == nameof(draft.Summary));
        Assert.Contains(result.Issues, issue => issue.Field == nameof(draft.Title));
        Assert.Contains(result.Issues, issue => issue.Field == nameof(draft.Categories));
        Assert.Contains(result.Issues, issue => issue.Field == nameof(draft.FinalThoughts));
    }

    [Fact]
    public void Validate_UnguidedBbCodeHasNoStructuredRequirements()
    {
        var draft = new ReviewDraft
        {
            EditingMode = ReviewEditingMode.UnguidedBbCode,
            Summary = string.Empty,
            Recommendation = null
        };

        var result = ReviewDraftValidator.Validate(draft);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_GuidedBbCodeOnlyRequiresSetup()
    {
        var draft = new ReviewDraft
        {
            EditingMode = ReviewEditingMode.GuidedBbCode,
            Summary = "Summary",
            Recommendation = ReviewRecommendation.Recommended,
            Title = string.Empty,
            FinalThoughts = string.Empty,
            Categories = []
        };

        var result = ReviewDraftValidator.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, issue => issue.Field == nameof(draft.Title));
    }

    [Fact]
    public void Validate_ReportsInvalidComponentAndTextRatings()
    {
        var draft = new ReviewDraft
        {
            Summary = "Summary",
            Recommendation = ReviewRecommendation.Recommended,
            RatingSystem = ReviewRatingSystem.Text,
            TextRatingOptions = ["Good", ""],
            Components =
            [
                new ReviewContentComponent
                {
                    Kind = ReviewContentComponentKind.Rating,
                    Heading = string.Empty,
                    Content = string.Empty,
                    Rating = 10
                }
            ]
        };

        var result = ReviewDraftValidator.Validate(draft);

        Assert.Contains(result.Issues, issue =>
            issue.Field == "TextRatingOption:1");
        Assert.Contains(result.Issues, issue =>
            issue.Field.StartsWith("ComponentHeading:", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue =>
            issue.Field.StartsWith("ComponentContent:", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue =>
            issue.Field.StartsWith("ComponentRating:", StringComparison.Ordinal));
    }
}
