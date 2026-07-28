using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class ReviewTemplateService
{
    public static void Apply(
        ReviewDraft draft,
        ReviewTemplate template)
    {
        ArgumentNullException.ThrowIfNull(draft);

        draft.Template = template;

        switch (template)
        {
            case ReviewTemplate.Balanced:
                ApplyBalancedTemplate(draft);
                break;

            case ReviewTemplate.QuickTake:
                ApplyQuickTakeTemplate(draft);
                break;

            case ReviewTemplate.DeepDive:
                ApplyDeepDiveTemplate(draft);
                break;

            case ReviewTemplate.Custom:
                ApplyCustomTemplate(draft);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(template),
                    template,
                    "Unknown review template.");
        }
    }

    private static void ApplyBalancedTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.RatingTable;

        draft.Summary =
            "A strong game with a few rough edges.";

        draft.WhatWorks =
            "Satisfying core gameplay\n" +
            "Lots of content to discover\n" +
            "Strong visual identity";

        draft.WhatCouldBeBetter =
            "Occasional performance issues\n" +
            "Some systems need clearer explanations";

        draft.FinalThoughts =
            "Despite its rough edges, the game delivers " +
            "a memorable and consistently enjoyable experience.";

        ReplaceCategories(
            draft,
            new ReviewCategory
            {
                Name = "Gameplay",
                Rating = 5,
                Note = "Responsive and consistently fun."
            },
            new ReviewCategory
            {
                Name = "Story",
                Rating = 3,
                Note = "Serviceable, but not the main attraction."
            },
            new ReviewCategory
            {
                Name = "Visuals",
                Rating = 4,
                Note = "Strong art direction and environments."
            });
    }

    private static void ApplyQuickTakeTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.MinimalVerdict;

        draft.Summary =
            "A concise summary of the overall experience.";

        draft.WhatWorks = string.Empty;
        draft.WhatCouldBeBetter = string.Empty;

        draft.FinalThoughts =
            "Briefly explain the main reason for your recommendation.";

        draft.Categories.Clear();
    }

    private static void ApplyDeepDiveTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.Sections;

        draft.Summary =
            "A detailed review of the game's major strengths and weaknesses.";

        draft.WhatWorks =
            "Describe the strongest gameplay systems\n" +
            "Describe the most successful presentation choices\n" +
            "Describe the content that kept you engaged";

        draft.WhatCouldBeBetter =
            "Describe mechanical or balance problems\n" +
            "Describe technical or performance issues\n" +
            "Describe content that felt incomplete";

        draft.FinalThoughts =
            "Summarize the complete experience and identify " +
            "the type of player most likely to enjoy it.";

        ReplaceCategories(
            draft,
            new ReviewCategory
            {
                Name = "Gameplay",
                Rating = 3,
                Note = string.Empty
            },
            new ReviewCategory
            {
                Name = "Story",
                Rating = 3,
                Note = string.Empty
            },
            new ReviewCategory
            {
                Name = "Visuals",
                Rating = 3,
                Note = string.Empty
            },
            new ReviewCategory
            {
                Name = "Audio",
                Rating = 3,
                Note = string.Empty
            },
            new ReviewCategory
            {
                Name = "Content",
                Rating = 3,
                Note = string.Empty
            },
            new ReviewCategory
            {
                Name = "Performance",
                Rating = 3,
                Note = string.Empty
            });
    }

    private static void ApplyCustomTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.RatingTable;

        draft.Summary = string.Empty;
        draft.WhatWorks = string.Empty;
        draft.WhatCouldBeBetter = string.Empty;
        draft.FinalThoughts = string.Empty;

        ReplaceCategories(
            draft,
            new ReviewCategory
            {
                Name = "New Category",
                Rating = 3,
                Note = string.Empty
            });
    }

    private static void ReplaceCategories(
        ReviewDraft draft,
        params ReviewCategory[] categories)
    {
        draft.Categories.Clear();
        draft.Categories.AddRange(categories);
    }
}