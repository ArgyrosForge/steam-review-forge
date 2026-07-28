using System.Text;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class SteamBbCodeGenerator
{
    public static string Generate(ReviewDraft draft)
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

        if (draft.DisplayFormat != ReviewDisplayFormat.MinimalVerdict &&
            draft.Categories.Count > 0)
        {
            AppendDivider(output);

            switch (draft.DisplayFormat)
            {
                case ReviewDisplayFormat.RatingTable:
                    AppendRatingTable(output, draft.Categories);
                    break;

                case ReviewDisplayFormat.Sections:
                    AppendSections(output, draft.Categories);
                    break;

                case ReviewDisplayFormat.Checklist:
                    AppendChecklist(output, draft.Categories);
                    break;
            }
        }

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

        AppendDivider(output);

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

        AppendDivider(output);

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
        IEnumerable<ReviewCategory> categories)
    {
        output.AppendLine("[table equalcells=1]");
        output.AppendLine(
            "[tr][th]Category[/th][th]Rating[/th][th]Note[/th][/tr]");

        foreach (var category in categories)
        {
            output.AppendLine(
                $"[tr][td][b]{GetCategoryName(category)}[/b][/td]" +
                $"[td]{FormatRating(category.Rating)}[/td]" +
                $"[td]{category.Note.Trim()}[/td][/tr]");
        }

        output.AppendLine("[/table]");
    }

    private static void AppendSections(
        StringBuilder output,
        IEnumerable<ReviewCategory> categories)
    {
        foreach (var category in categories)
        {
            output.AppendLine($"[h2]{GetCategoryName(category)}[/h2]");
            output.AppendLine(FormatRating(category.Rating));

            if (!string.IsNullOrWhiteSpace(category.Note))
            {
                output.AppendLine(category.Note.Trim());
            }

            output.AppendLine();
        }
    }

    private static void AppendChecklist(
        StringBuilder output,
        IEnumerable<ReviewCategory> categories)
    {
        foreach (var category in categories)
        {
            var marker = category.Rating >= 4 ? "☑" : "☐";

            var note = string.IsNullOrWhiteSpace(category.Note)
                ? string.Empty
                : $" — {category.Note.Trim()}";

            output.AppendLine(
                $"{marker} [b]{GetCategoryName(category)}[/b] " +
                $"— {FormatRating(category.Rating)}{note}");
        }
    }

    private static void AppendDivider(StringBuilder output)
    {
        output.AppendLine();
        output.AppendLine("[hr][/hr]");
        output.AppendLine();
    }

    private static string GetCategoryName(ReviewCategory category)
    {
        return string.IsNullOrWhiteSpace(category.Name)
            ? "Category"
            : category.Name.Trim();
    }

    private static string FormatRating(int rating)
    {
        var normalizedRating = Math.Clamp(rating, 1, 5);

        return new string('★', normalizedRating) +
               new string('☆', 5 - normalizedRating);
    }
}
