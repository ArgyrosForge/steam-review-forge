using System.Text;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public static class SteamBbCodeGenerator
{
    public static string Generate(ReviewDraft draft)
    {
        var title = string.IsNullOrWhiteSpace(draft.Title)
            ? "Untitled Review"
            : draft.Title.Trim();

        var output = new StringBuilder();

        output.AppendLine($"[h1]{title}[/h1]");

        if (!string.IsNullOrWhiteSpace(draft.Summary))
        {
            output.AppendLine($"[i]{draft.Summary.Trim()}[/i]");
        }

        if (draft.Categories.Count > 0)
        {
            output.AppendLine();
            output.AppendLine("[hr][/hr]");
            output.AppendLine();
            output.AppendLine("[table equalcells=1]");
            output.AppendLine(
                "[tr][th]Category[/th][th]Rating[/th][th]Note[/th][/tr]");

            foreach (var category in draft.Categories)
            {
                var name = string.IsNullOrWhiteSpace(category.Name)
                    ? "Category"
                    : category.Name.Trim();

                var note = category.Note.Trim();

                output.AppendLine(
                    $"[tr][td][b]{name}[/b][/td]" +
                    $"[td]{FormatRating(category.Rating)}[/td]" +
                    $"[td]{note}[/td][/tr]");
            }

            output.AppendLine("[/table]");
        }

        output.AppendLine();
        output.AppendLine("[hr][/hr]");
        output.AppendLine();
        output.AppendLine(
            $"[b]Recommendation:[/b] " +
            $"{FormatRecommendation(draft.Recommendation)}");

        return output.ToString().Trim();
    }

    private static string FormatRating(int rating)
    {
        var normalizedRating = Math.Clamp(rating, 1, 5);

        return new string('★', normalizedRating) +
               new string('☆', 5 - normalizedRating);
    }

    private static string FormatRecommendation(
        ReviewRecommendation recommendation)
    {
        return recommendation switch
        {
            ReviewRecommendation.Recommended => "Recommended",
            ReviewRecommendation.Mixed => "Mixed",
            ReviewRecommendation.NotRecommended => "Not Recommended",
            _ => "Unspecified"
        };
    }
}