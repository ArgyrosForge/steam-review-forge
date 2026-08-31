using System.Text;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class SteamBbCodeGenerator
{
    public static string Generate(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var output = new StringBuilder();

        AppendGeneratedSection(
            output,
            GenerateIntro(draft),
            includeDivider: false);
        AppendGeneratedSection(
            output,
            GenerateCategories(draft),
            draft.IncludeCategoryDivider);
        AppendGeneratedSection(
            output,
            GenerateComponents(draft),
            draft.IncludeComponentDivider);
        AppendGeneratedSection(
            output,
            GenerateBody(draft),
            draft.IncludeWritingDivider);

        return output.ToString();
    }

    public static string GenerateIntro(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var output = new StringBuilder();

        if (draft.IncludeTitle)
        {
            var title = string.IsNullOrWhiteSpace(draft.Title)
                ? "Untitled Review"
                : draft.Title.Trim();

            output.AppendLine($"[h1]{title}[/h1]");
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
                    draft.TableCellWidthMode,
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

    public static string GenerateComponents(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var components = draft.Components
            .Select(component => GenerateComponent(draft, component))
            .Where(component => !string.IsNullOrWhiteSpace(component));

        return string.Join(
            "\n\n[hr][/hr]\n\n",
            components);
    }

    private static string GenerateComponent(
        ReviewDraft draft,
        ReviewContentComponent component)
    {
        var heading = string.IsNullOrWhiteSpace(component.Heading)
            ? "New Section"
            : component.Heading.Trim();
        var output = new StringBuilder();

        output.AppendLine($"[h2]{heading}[/h2]");

        if (component.Kind == ReviewContentComponentKind.Rating)
        {
            output.AppendLine(
                FormatRating(
                    component.Rating,
                    draft.RatingSystem,
                    draft.TextRatingOptions));
        }

        AppendFormattedContent(
            output,
            component.Content,
            component.ContentFormat);

        return output.ToString().Trim();
    }

    private static void AppendQuestionnaire(
        StringBuilder output,
        ReviewDraft draft)
    {
        if (!draft.IncludeWhatWorks &&
            !draft.IncludeWhatCouldBeBetter &&
            !draft.IncludeFinalThoughts)
        {
            return;
        }

        if (draft.IncludeWhatWorks)
        {
            AppendFormattedSection(
                output,
                draft.WhatWorksHeading,
                draft.WhatWorks,
                draft.WhatWorksFormat);
        }

        if (draft.IncludeWhatCouldBeBetter)
        {
            AppendFormattedSection(
                output,
                draft.WhatCouldBeBetterHeading,
                draft.WhatCouldBeBetter,
                draft.WhatCouldBeBetterFormat);
        }

        if (draft.IncludeFinalThoughts)
        {
            AppendFormattedSection(
                output,
                draft.FinalThoughtsHeading,
                draft.FinalThoughts,
                draft.FinalThoughtsFormat);
        }
    }

    private static void AppendMinimalDetails(
        StringBuilder output,
        ReviewDraft draft)
    {
        if (!draft.IncludeFinalThoughts)
        {
            return;
        }

        AppendFormattedSection(
            output,
            draft.FinalThoughtsHeading,
            draft.FinalThoughts,
            draft.FinalThoughtsFormat);
    }

    private static void AppendFormattedSection(
        StringBuilder output,
        string heading,
        string content,
        ReviewTextFormat format)
    {
        if (!string.IsNullOrWhiteSpace(heading))
        {
            output.AppendLine($"[h2]{heading.Trim()}[/h2]");
        }

        AppendFormattedContent(output, content, format);

        output.AppendLine();
    }

    private static void AppendFormattedContent(
        StringBuilder output,
        string content,
        ReviewTextFormat format)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (format == ReviewTextFormat.Text)
        {
            output.AppendLine(content.Trim());
            return;
        }

        output.AppendLine(
            format == ReviewTextFormat.NumberedList
                ? "[olist]"
                : "[list]");

        foreach (var item in GetLines(content))
        {
            output.AppendLine($"[*]{item}");
        }

        output.AppendLine(
            format == ReviewTextFormat.NumberedList
                ? "[/olist]"
                : "[/list]");
    }

    private static void AppendGeneratedSection(
        StringBuilder output,
        string section,
        bool includeDivider)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return;
        }

        if (output.Length > 0)
        {
            output.Append(
                includeDivider
                    ? "\n\n[hr][/hr]\n\n"
                    : "\n\n");
        }

        output.Append(section.Trim());
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
        ReviewTableCellWidthMode cellWidthMode,
        ReviewRatingSystem ratingSystem,
        IReadOnlyList<string> textRatingOptions)
    {
        if (columns.Count == 0)
        {
            return;
        }

        output.AppendLine(
            cellWidthMode == ReviewTableCellWidthMode.Equal
                ? "[table equalcells=1]"
                : "[table]");

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
