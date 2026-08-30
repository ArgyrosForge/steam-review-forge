using SteamReviewForge.Models;
using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class SteamBbCodeGeneratorTests
{
    [Theory]
    [InlineData(ReviewDisplayFormat.RatingTable, "[table equalcells=1]")]
    [InlineData(ReviewDisplayFormat.Sections, "[h2]Gameplay[/h2]")]
    [InlineData(ReviewDisplayFormat.Checklist, "☑ [b]Gameplay[/b]")]
    [InlineData(ReviewDisplayFormat.MinimalVerdict, "[h2]Why[/h2]")]
    public void Generate_EmitsExpectedFormat(
        ReviewDisplayFormat format,
        string expected)
    {
        var draft = CreateCompleteDraft();
        draft.DisplayFormat = format;
        draft.Categories[0].Rating = 5;

        if (format == ReviewDisplayFormat.MinimalVerdict)
        {
            draft.FinalThoughtsHeading = "Why";
        }

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Contains(expected, output);
        Assert.Equal(output, SteamBbCodeGenerator.Generate(draft));
    }

    [Theory]
    [InlineData(ReviewRatingSystem.FiveStars, "★★★★☆")]
    [InlineData(ReviewRatingSystem.FivePoint, "4/5")]
    [InlineData(ReviewRatingSystem.TenPoint, "4/10")]
    [InlineData(ReviewRatingSystem.Text, "Good")]
    public void Generate_FormatsEachRatingSystem(
        ReviewRatingSystem ratingSystem,
        string expected)
    {
        var draft = CreateCompleteDraft();
        draft.RatingSystem = ratingSystem;
        draft.Categories[0].Rating = 4;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Contains(expected, output);
    }

    [Fact]
    public void Generate_ExcludesSteamOwnedMetadata()
    {
        var draft = CreateCompleteDraft();
        draft.Playtime = "123.4";
        draft.ReceivedProductForFree = true;
        draft.IsEarlyAccessReview = true;
        draft.Recommendation = ReviewRecommendation.NotRecommended;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.DoesNotContain("123.4", output);
        Assert.DoesNotContain("Product received", output);
        Assert.DoesNotContain("Early Access Review", output);
        Assert.DoesNotContain("Not Recommended", output);
    }

    [Fact]
    public void Generate_IncludesIndependentComponentsInOrder()
    {
        var draft = CreateCompleteDraft();
        draft.Components =
        [
            new ReviewContentComponent
            {
                Kind = ReviewContentComponentKind.Text,
                Heading = "First component",
                Content = "First content"
            },
            new ReviewContentComponent
            {
                Kind = ReviewContentComponentKind.BulletedList,
                Heading = "Second component",
                Content = "One\nTwo"
            }
        ];

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.True(
            output.IndexOf("First component", StringComparison.Ordinal) <
            output.IndexOf("Second component", StringComparison.Ordinal));
        Assert.Contains("• One", output);
        Assert.Contains("• Two", output);
    }

    [Fact]
    public void Generate_ExcludesRemovedStructuredBlocks()
    {
        var draft = CreateCompleteDraft();
        draft.IncludeTitle = false;
        draft.IncludeSummary = false;
        draft.IncludeWhatWorks = false;
        draft.IncludeWhatCouldBeBetter = false;
        draft.IncludeFinalThoughts = false;
        draft.Categories.Clear();

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Empty(output);
    }

    [Fact]
    public void Generate_UsesEditableHeadingsAndOptionalDividers()
    {
        var draft = CreateCompleteDraft();
        draft.WhatWorksHeading = "Highlights";
        draft.WhatCouldBeBetterHeading = "Rough Edges";
        draft.FinalThoughtsHeading = "Verdict";
        draft.IncludeCategoryDivider = false;
        draft.IncludeWritingDivider = false;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Contains("[h2]Highlights[/h2]", output);
        Assert.Contains("[h2]Rough Edges[/h2]", output);
        Assert.Contains("[h2]Verdict[/h2]", output);
        Assert.DoesNotContain("[hr][/hr]", output);
    }

    private static ReviewDraft CreateCompleteDraft()
    {
        return new ReviewDraft
        {
            Title = "Test Review",
            Summary = "A concise summary.",
            Recommendation = ReviewRecommendation.Recommended,
            FinalThoughts = "Final verdict.",
            WhatWorks = "Strong systems",
            WhatCouldBeBetter = "Minor issues",
            Categories =
            [
                new ReviewCategory
                {
                    Name = "Gameplay",
                    Rating = 4,
                    Note = "Responsive"
                }
            ]
        };
    }
}
