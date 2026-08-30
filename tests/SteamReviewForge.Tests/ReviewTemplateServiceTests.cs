using SteamReviewForge.Models;
using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class ReviewTemplateServiceTests
{
    [Theory]
    [InlineData(ReviewTemplate.Balanced, ReviewDisplayFormat.RatingTable, 3)]
    [InlineData(ReviewTemplate.QuickTake, ReviewDisplayFormat.MinimalVerdict, 0)]
    [InlineData(ReviewTemplate.DeepDive, ReviewDisplayFormat.Sections, 6)]
    [InlineData(ReviewTemplate.Custom, ReviewDisplayFormat.RatingTable, 1)]
    public void Apply_ConfiguresExpectedTemplateShape(
        ReviewTemplate template,
        ReviewDisplayFormat expectedFormat,
        int expectedCategoryCount)
    {
        var draft = new ReviewDraft();

        ReviewTemplateService.Apply(draft, template);

        Assert.Equal(template, draft.Template);
        Assert.Equal(expectedFormat, draft.DisplayFormat);
        Assert.Equal(expectedCategoryCount, draft.Categories.Count);
        Assert.Equal(3, draft.TableColumns.Count);
    }

    [Fact]
    public void Apply_PreservesSetupMetadataAndTitle()
    {
        var draft = new ReviewDraft
        {
            Title = "My title",
            Summary = "My summary",
            Recommendation = ReviewRecommendation.NotRecommended,
            Playtime = "42.5",
            ReceivedProductForFree = true,
            IsEarlyAccessReview = true
        };

        ReviewTemplateService.Apply(draft, ReviewTemplate.DeepDive);

        Assert.Equal("My title", draft.Title);
        Assert.Equal("My summary", draft.Summary);
        Assert.Equal(ReviewRecommendation.NotRecommended, draft.Recommendation);
        Assert.Equal("42.5", draft.Playtime);
        Assert.True(draft.ReceivedProductForFree);
        Assert.True(draft.IsEarlyAccessReview);
    }

    [Fact]
    public void Apply_DeepDiveContainsOnlySectionCategories()
    {
        var draft = new ReviewDraft();

        ReviewTemplateService.Apply(draft, ReviewTemplate.DeepDive);

        Assert.Equal(ReviewDisplayFormat.Sections, draft.DisplayFormat);
        Assert.Equal(6, draft.Categories.Count);
        Assert.False(draft.IncludeWhatWorks);
        Assert.False(draft.IncludeWhatCouldBeBetter);
        Assert.False(draft.IncludeFinalThoughts);
        Assert.Empty(draft.WhatWorks);
        Assert.Empty(draft.WhatCouldBeBetter);
        Assert.Empty(draft.FinalThoughts);
    }

    [Fact]
    public void Apply_RemapsRatingsForTenPointScale()
    {
        var draft = new ReviewDraft
        {
            RatingSystem = ReviewRatingSystem.TenPoint
        };

        ReviewTemplateService.Apply(draft, ReviewTemplate.Balanced);

        Assert.Equal([10, 6, 8], draft.Categories.Select(category => category.Rating));
    }

    [Theory]
    [InlineData(ReviewTemplate.Balanced, "Final Thoughts")]
    [InlineData(ReviewTemplate.QuickTake, "Why")]
    public void Apply_ResetsEditableHeadingsAndDividers(
        ReviewTemplate template,
        string expectedFinalHeading)
    {
        var draft = new ReviewDraft
        {
            WhatWorksHeading = "Changed",
            WhatCouldBeBetterHeading = "Changed",
            FinalThoughtsHeading = "Changed",
            IncludeCategoryDivider = false,
            IncludeComponentDivider = false,
            IncludeWritingDivider = false
        };

        ReviewTemplateService.Apply(draft, template);

        Assert.Equal("What Works", draft.WhatWorksHeading);
        Assert.Equal("What Could Be Better", draft.WhatCouldBeBetterHeading);
        Assert.Equal(expectedFinalHeading, draft.FinalThoughtsHeading);
        Assert.True(draft.IncludeCategoryDivider);
        Assert.True(draft.IncludeComponentDivider);
        Assert.True(draft.IncludeWritingDivider);
    }
}
