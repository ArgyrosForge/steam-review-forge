using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class ReviewDraftValidator
{
    public static ReviewValidationResult Validate(
        ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var result = new ReviewValidationResult();

        ValidateSetup(draft, result);
        ValidateFormat(draft, result);
        ValidateQuestions(draft, result);

        return result;
    }

    private static void ValidateSetup(
        ReviewDraft draft,
        ReviewValidationResult result)
    {
        if (draft.Recommendation is null)
            AddError(
                result,
                ReviewValidationSection.Setup,
                nameof(draft.Recommendation),
                "Choose Recommended or Not Recommended.");

        if (string.IsNullOrWhiteSpace(draft.Title))
            AddError(
                result,
                ReviewValidationSection.Setup,
                nameof(draft.Title),
                "Enter a review title.");

        if (string.IsNullOrWhiteSpace(draft.Summary))
            AddError(
                result,
                ReviewValidationSection.Setup,
                nameof(draft.Summary),
                "Add a short sentence that summarizes your review.");

        if (string.IsNullOrWhiteSpace(draft.Playtime))
            AddWarning(
                result,
                ReviewValidationSection.Setup,
                nameof(draft.Playtime),
                "Consider including your approximate playtime.");
    }

    private static void ValidateFormat(
        ReviewDraft draft,
        ReviewValidationResult result)
    {
        if (draft.DisplayFormat ==
            ReviewDisplayFormat.MinimalVerdict)
            return;

        if (draft.Categories.Count == 0)
        {
            AddError(
                result,
                ReviewValidationSection.Format,
                nameof(draft.Categories),
                "Add at least one review category.");

            return;
        }

        for (var index = 0;
             index < draft.Categories.Count;
             index++)
        {
            var category = draft.Categories[index];

            if (string.IsNullOrWhiteSpace(category.Name))
                AddError(
                    result,
                    ReviewValidationSection.Format,
                    $"CategoryName:{category.Id}",
                    $"Category {index + 1} needs a name.");

            if (string.IsNullOrWhiteSpace(category.Note))
                AddWarning(
                    result,
                    ReviewValidationSection.Format,
                    $"CategoryNote:{category.Id}",
                    $"{GetCategoryLabel(category, index)} has no note.");
        }
    }

    private static void ValidateQuestions(
        ReviewDraft draft,
        ReviewValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(
                draft.FinalThoughts))
            AddError(
                result,
                ReviewValidationSection.Questions,
                nameof(draft.FinalThoughts),
                draft.DisplayFormat ==
                ReviewDisplayFormat.MinimalVerdict
                    ? "Explain the reason for your verdict."
                    : "Add your final thoughts.");

        if (draft.DisplayFormat ==
            ReviewDisplayFormat.MinimalVerdict)
            return;

        if (string.IsNullOrWhiteSpace(draft.WhatWorks))
            AddWarning(
                result,
                ReviewValidationSection.Questions,
                nameof(draft.WhatWorks),
                "Consider adding at least one strength.");

        if (string.IsNullOrWhiteSpace(
                draft.WhatCouldBeBetter))
            AddWarning(
                result,
                ReviewValidationSection.Questions,
                nameof(draft.WhatCouldBeBetter),
                "Consider adding at least one weakness.");
    }

    private static string GetCategoryLabel(
        ReviewCategory category,
        int index)
    {
        return string.IsNullOrWhiteSpace(category.Name)
            ? $"Category {index + 1}"
            : category.Name.Trim();
    }

    private static void AddError(
        ReviewValidationResult result,
        ReviewValidationSection section,
        string field,
        string message)
    {
        result.Issues.Add(
            new ReviewValidationIssue(
                section,
                field,
                message,
                ReviewValidationSeverity.Error));
    }

    private static void AddWarning(
        ReviewValidationResult result,
        ReviewValidationSection section,
        string field,
        string message)
    {
        result.Issues.Add(
            new ReviewValidationIssue(
                section,
                field,
                message,
                ReviewValidationSeverity.Warning));
    }
}
