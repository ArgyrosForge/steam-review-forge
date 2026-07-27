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

        output.AppendLine();
        output.AppendLine("[hr][/hr]");
        output.AppendLine();
        output.AppendLine(
            $"[b]Recommendation:[/b] {FormatRecommendation(draft.Recommendation)}");

        return output.ToString().Trim();
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
