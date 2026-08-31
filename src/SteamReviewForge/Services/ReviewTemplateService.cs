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
        draft.TableCellWidthMode = ReviewTableCellWidthMode.Equal;
        ResetTableColumns(draft);
        ResetStructuredLabels(draft);

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

        var targetMaximum =
            draft.RatingSystem switch
            {
                ReviewRatingSystem.TenPoint => 10,
                ReviewRatingSystem.Text =>
                    Math.Max(
                        1,
                        draft.TextRatingOptions.Count),
                _ => 5
            };

        if (targetMaximum != 5)
        {
            foreach (var category in draft.Categories)
            {
                category.Rating = Math.Clamp(
                    (int)Math.Round(
                        1d +
                        (category.Rating - 1d) /
                        4d *
                        (targetMaximum - 1d),
                        MidpointRounding.AwayFromZero),
                    1,
                    targetMaximum);
            }
        }
    }

    private static void ApplyBalancedTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.RatingTable;
        draft.IncludeWhatWorks = true;
        draft.IncludeWhatCouldBeBetter = true;
        draft.IncludeFinalThoughts = true;

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
        draft.IncludeWhatWorks = false;
        draft.IncludeWhatCouldBeBetter = false;
        draft.IncludeFinalThoughts = true;

        draft.WhatWorks = string.Empty;
        draft.WhatCouldBeBetter = string.Empty;

        draft.FinalThoughts =
            "Briefly explain the main reason for your recommendation.";
        draft.FinalThoughtsHeading = "Why";

        draft.Categories.Clear();
    }

    private static void ApplyDeepDiveTemplate(ReviewDraft draft)
    {
        draft.DisplayFormat = ReviewDisplayFormat.Sections;
        draft.IncludeWhatWorks = false;
        draft.IncludeWhatCouldBeBetter = false;
        draft.IncludeFinalThoughts = false;
        draft.WhatWorks = string.Empty;
        draft.WhatCouldBeBetter = string.Empty;
        draft.FinalThoughts = string.Empty;

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
        draft.IncludeWhatWorks = true;
        draft.IncludeWhatCouldBeBetter = true;
        draft.IncludeFinalThoughts = true;

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

    private static void ResetTableColumns(
        ReviewDraft draft)
    {
        draft.TableColumns =
        [
            new ReviewTableColumn
            {
                Heading = "Category",
                Kind =
                    ReviewTableColumnKind.Category
            },
            new ReviewTableColumn
            {
                Heading = "Rating",
                Kind =
                    ReviewTableColumnKind.Rating
            },
            new ReviewTableColumn
            {
                Heading = "Notes",
                Kind =
                    ReviewTableColumnKind.Note
            }
        ];
    }

    private static void ResetStructuredLabels(
        ReviewDraft draft)
    {
        draft.WhatWorksHeading = "What Works";
        draft.WhatCouldBeBetterHeading = "What Could Be Better";
        draft.FinalThoughtsHeading = "Final Thoughts";
        draft.WhatWorksFormat = ReviewTextFormat.Text;
        draft.WhatCouldBeBetterFormat = ReviewTextFormat.Text;
        draft.FinalThoughtsFormat = ReviewTextFormat.Text;
        draft.IncludeCategoryDivider = true;
        draft.IncludeComponentDivider = true;
        draft.IncludeWritingDivider = true;
    }
}
