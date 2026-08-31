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
    public void Generate_UsesSelectedTableCellWidthMode()
    {
        var draft = CreateCompleteDraft();
        draft.TableCellWidthMode = ReviewTableCellWidthMode.Automatic;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Contains("[table]\n", output);
        Assert.DoesNotContain("[table equalcells=1]", output);
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
                ContentFormat = ReviewTextFormat.BulletedList,
                Heading = "Second component",
                Content = "One\nTwo"
            }
        ];

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.True(
            output.IndexOf("First component", StringComparison.Ordinal) <
            output.IndexOf("Second component", StringComparison.Ordinal));
        Assert.Contains("[list]\n[*]One\n[*]Two\n[/list]", output);
    }

    [Fact]
    public void Generate_UsesIndependentTextAndListFormatsForEveryBody()
    {
        var draft = CreateCompleteDraft();
        draft.WhatWorks = "One\nTwo";
        draft.WhatWorksFormat = ReviewTextFormat.BulletedList;
        draft.WhatCouldBeBetter = "Three\nFour";
        draft.WhatCouldBeBetterFormat = ReviewTextFormat.NumberedList;
        draft.FinalThoughts = "Plain\ntext";
        draft.FinalThoughtsFormat = ReviewTextFormat.Text;
        draft.Components =
        [
            new ReviewContentComponent
            {
                Kind = ReviewContentComponentKind.Rating,
                Heading = "Combat",
                Content = "Fast\nReadable",
                ContentFormat = ReviewTextFormat.NumberedList,
                Rating = 4
            }
        ];

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Contains("[list]\n[*]One\n[*]Two\n[/list]", output);
        Assert.Contains("[olist]\n[*]Three\n[*]Four\n[/olist]", output);
        Assert.Contains("[h2]Final Thoughts[/h2]\nPlain\ntext", output);
        Assert.Contains(
            "[h2]Combat[/h2]\n★★★★☆\n[olist]\n[*]Fast\n[*]Readable\n[/olist]",
            output);
    }

    [Fact]
    public void Generate_DefaultStructuredBodiesArePlainText()
    {
        var draft = CreateCompleteDraft();

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.DoesNotContain("[list]", output);
        Assert.DoesNotContain("[olist]", output);
        Assert.DoesNotContain("• Strong systems", output);
        Assert.Contains("[h2]What Works[/h2]\nStrong systems", output);
    }

    [Fact]
    public void Generate_ExcludesRemovedStructuredBlocks()
    {
        var draft = CreateCompleteDraft();
        draft.IncludeTitle = false;
        draft.IncludeWhatWorks = false;
        draft.IncludeWhatCouldBeBetter = false;
        draft.IncludeFinalThoughts = false;
        draft.Categories.Clear();

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.Empty(output);
    }

    [Fact]
    public void Generate_PreservesIncludedEmptyStructuredBlocks()
    {
        var draft = CreateCompleteDraft();
        draft.WhatWorks = string.Empty;
        draft.WhatCouldBeBetter = string.Empty;
        draft.FinalThoughts = string.Empty;

        var output = SteamBbCodeGenerator.Generate(draft);

        Assert.DoesNotContain("[i]", output);
        Assert.Contains("[h2]What Works[/h2]", output);
        Assert.Contains("[h2]What Could Be Better[/h2]", output);
        Assert.Contains("[h2]Final Thoughts[/h2]", output);
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
