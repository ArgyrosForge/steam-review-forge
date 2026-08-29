using SteamReviewForge.Models;

namespace SteamReviewForge.Tests;

public sealed class ReviewValidationResultTests
{
    [Fact]
    public void IsValid_IsTrue_WhenThereAreNoErrors()
    {
        var result = new ReviewValidationResult();

        result.Issues.Add(new ReviewValidationIssue(
            ReviewValidationSection.Setup,
            "Title",
            "Title could be more descriptive.",
            ReviewValidationSeverity.Warning));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsValid_IsFalse_WhenAnErrorExists()
    {
        var result = new ReviewValidationResult();

        result.Issues.Add(new ReviewValidationIssue(
            ReviewValidationSection.Setup,
            "Title",
            "Title is required.",
            ReviewValidationSeverity.Error));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void HasErrors_OnlyMatchesRequestedSection()
    {
        var result = new ReviewValidationResult();

        result.Issues.Add(new ReviewValidationIssue(
            ReviewValidationSection.Template,
            "Template",
            "Template is required.",
            ReviewValidationSeverity.Error));

        Assert.True(result.HasErrors(ReviewValidationSection.Template));
        Assert.False(result.HasErrors(ReviewValidationSection.Setup));
    }

    [Fact]
    public void ForField_ReturnsOnlyExactFieldMatches()
    {
        var result = new ReviewValidationResult();

        result.Issues.Add(new ReviewValidationIssue(
            ReviewValidationSection.Setup,
            "Title",
            "Title is required.",
            ReviewValidationSeverity.Error));
        result.Issues.Add(new ReviewValidationIssue(
            ReviewValidationSection.Setup,
            "Summary",
            "Summary is required.",
            ReviewValidationSeverity.Error));

        var matches = result.ForField("Title").ToList();

        Assert.Single(matches);
        Assert.Equal("Title", matches[0].Field);
    }
}
