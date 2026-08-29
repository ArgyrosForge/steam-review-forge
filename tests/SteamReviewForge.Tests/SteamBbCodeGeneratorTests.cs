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
        draft.Recommendation = ReviewRecommendation.NotRecommended;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.DoesNotContain("123.4", output);
        Assert.DoesNotContain("Product received", output);
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
