using System.Text;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class SteamBbCodeGenerator
{
    public static string GenerateSetupPreview(
        ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return string.IsNullOrWhiteSpace(draft.Summary)
            ? string.Empty
            : draft.Summary.Trim();
    }

    public static string Generate(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        string[] sections =
        [
            GenerateIntro(draft),
            GenerateCategories(draft),
            GenerateBody(draft)
        ];

        return string.Join(
            "\n\n[hr][/hr]\n\n",
            sections.Where(
                section => !string.IsNullOrWhiteSpace(section)));
    }

    public static string GenerateIntro(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var title = string.IsNullOrWhiteSpace(draft.Title)
            ? "Untitled Review"
            : draft.Title.Trim();

        var output = new StringBuilder();

        output.AppendLine($"[h1]{title}[/h1]");

        if (!string.IsNullOrWhiteSpace(draft.Summary))
        {
            output.AppendLine($"[i]{draft.Summary.Trim()}[/i]");
        }

        return output.ToString().Trim();
    }

    public static string GenerateCategories(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.DisplayFormat == ReviewDisplayFormat.MinimalVerdict ||
            draft.Categories.Count == 0)
        {
            return string.Empty;
        }

        var output = new StringBuilder();

        switch (draft.DisplayFormat)
        {
            case ReviewDisplayFormat.RatingTable:
                AppendRatingTable(
                    output,
                    draft.Categories,
                    draft.TableColumns,
                    draft.RatingSystem,
                    draft.TextRatingOptions);
                break;

            case ReviewDisplayFormat.Sections:
                AppendSections(
                    output,
                    draft.Categories,
                    draft.RatingSystem,
                    draft.TextRatingOptions);
                break;

            case ReviewDisplayFormat.Checklist:
                AppendChecklist(
                    output,
                    draft.Categories,
                    draft.RatingSystem,
                    draft.TextRatingOptions);
                break;
        }

        return output.ToString().Trim();
    }

    public static string GenerateBody(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var output = new StringBuilder();

        if (draft.DisplayFormat == ReviewDisplayFormat.MinimalVerdict)
        {
            AppendMinimalDetails(output, draft);
        }
        else
        {
            AppendQuestionnaire(output, draft);
        }

        return output.ToString().Trim();
    }

    private static void AppendQuestionnaire(
        StringBuilder output,
        ReviewDraft draft)
    {
        var whatWorks = GetLines(draft.WhatWorks);
        var whatCouldBeBetter = GetLines(draft.WhatCouldBeBetter);
        var hasFinalThoughts =
            !string.IsNullOrWhiteSpace(draft.FinalThoughts);

        if (whatWorks.Length == 0 &&
            whatCouldBeBetter.Length == 0 &&
            !hasFinalThoughts)
        {
            return;
        }

        AppendListSection(output, "What Works", whatWorks);

        AppendListSection(
            output,
            "What Could Be Better",
            whatCouldBeBetter);

        if (hasFinalThoughts)
        {
            AppendTextSection(
                output,
                "Final Thoughts",
                draft.FinalThoughts.Trim());
        }
    }

    private static void AppendMinimalDetails(
        StringBuilder output,
        ReviewDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.FinalThoughts))
        {
            return;
        }

        AppendTextSection(
            output,
            "Why",
            draft.FinalThoughts.Trim());
    }

    private static void AppendListSection(
        StringBuilder output,
        string heading,
        IEnumerable<string> items)
    {
        var itemList = items.ToArray();

        if (itemList.Length == 0)
        {
            return;
        }

        output.AppendLine($"[h2]{heading}[/h2]");

        foreach (var item in itemList)
        {
            output.AppendLine($"• {item}");
        }

        output.AppendLine();
    }

    private static void AppendTextSection(
        StringBuilder output,
        string heading,
        string text)
    {
        output.AppendLine($"[h2]{heading}[/h2]");
        output.AppendLine(text);
        output.AppendLine();
    }

    private static string[] GetLines(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private static void AppendRatingTable(
        StringBuilder output,
        IEnumerable<ReviewCategory> categories,
        IReadOnlyList<ReviewTableColumn> columns,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        if (columns.Count == 0)
        {
            return;
        }

        output.AppendLine("[table equalcells=1]");

        var headings = new StringBuilder("[tr]");

        foreach (var column in columns)
            headings.Append(
                $"[th]{GetColumnHeading(column)}[/th]");

        headings.Append("[/tr]");
        output.AppendLine(headings.ToString());

        foreach (var category in categories)
        {
            var row = new StringBuilder("[tr]");

            foreach (var column in columns)
            {
                row.Append("[td]");
                row.Append(
                    GetCellValue(
                        category,
                        column,
                        ratingSystem,
                        textRatingOptions));
                row.Append("[/td]");
            }

            row.Append("[/tr]");
            output.AppendLine(row.ToString());
        }

        output.AppendLine("[/table]");
    }

    private static string GetColumnHeading(
        ReviewTableColumn column)
    {
        return string.IsNullOrWhiteSpace(column.Heading)
            ? "Column"
            : column.Heading.Trim();
    }

    private static string GetCellValue(
        ReviewCategory category,
        ReviewTableColumn column,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        return column.Kind switch
        {
            ReviewTableColumnKind.Category =>
                $"[b]{GetCategoryName(category)}[/b]",

            ReviewTableColumnKind.Rating =>
                FormatRating(
                    category.Rating,
                    ratingSystem,
                    textRatingOptions),

            ReviewTableColumnKind.Note =>
                category.Note.Trim(),

            ReviewTableColumnKind.CustomText =>
                category.CustomCells.TryGetValue(
                    column.Id,
                    out var value)
                    ? value.Trim()
                    : string.Empty,

            _ => string.Empty
        };
    }

    private static void AppendSections(
        StringBuilder output,
        IEnumerable<ReviewCategory> categories,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        foreach (var category in categories)
        {
            output.AppendLine($"[h2]{GetCategoryName(category)}[/h2]");
            output.AppendLine(
                FormatRating(
                    category.Rating,
                    ratingSystem,
                    textRatingOptions));

            if (!string.IsNullOrWhiteSpace(category.Note))
            {
                output.AppendLine(category.Note.Trim());
            }

            output.AppendLine();
        }
    }

    private static void AppendChecklist(
        StringBuilder output,
        IEnumerable<ReviewCategory> categories,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        foreach (var category in categories)
        {
            var marker =
                IsPositiveRating(
                    category.Rating,
                    ratingSystem,
                    textRatingOptions.Count)
                    ? "☑"
                    : "☐";

            var note = string.IsNullOrWhiteSpace(category.Note)
                ? string.Empty
                : $" — {category.Note.Trim()}";

            output.AppendLine(
                $"{marker} [b]{GetCategoryName(category)}[/b] " +
                $"— {FormatRating(category.Rating, ratingSystem, textRatingOptions)}{note}");
        }
    }

    private static string GetCategoryName(ReviewCategory category)
    {
        return string.IsNullOrWhiteSpace(category.Name)
            ? "Category"
            : category.Name.Trim();
    }

    private static string FormatRating(
        int rating,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        if (ratingSystem == ReviewRatingSystem.TenPoint)
        {
            return $"{Math.Clamp(rating, 1, 10)}/10";
        }

        var normalizedRating = Math.Clamp(rating, 1, 5);

        return ratingSystem switch
        {
            ReviewRatingSystem.FivePoint =>
                $"{normalizedRating}/5",

            ReviewRatingSystem.Text =>
                GetTextRating(
                    rating,
                    textRatingOptions),

            _ =>
                new string('★', normalizedRating) +
                new string('☆', 5 - normalizedRating)
        };
    }

    private static bool IsPositiveRating(
        int rating,
        ReviewRatingSystem ratingSystem,
        int textRatingCount)
    {
        var threshold =
            ratingSystem switch
            {
                ReviewRatingSystem.TenPoint => 8,
                ReviewRatingSystem.Text =>
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            Math.Max(
                                1,
                                textRatingCount) *
                            0.7d)),
                _ => 4
            };

        return rating >= threshold;
    }

    private static string GetTextRating(
        int rating,
        IReadOnlyList<string> textRatingOptions)
    {
        if (textRatingOptions.Count == 0)
        {
            return "Rating";
        }

        var index =
            Math.Clamp(
                rating,
                1,
                textRatingOptions.Count) -
            1;

        return string.IsNullOrWhiteSpace(
            textRatingOptions[index])
                ? $"Rating {index + 1}"
                : textRatingOptions[index].Trim();
    }
}
