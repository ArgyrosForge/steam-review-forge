namespace SteamReviewForge.Models;

public sealed class ReviewDraft
{
    public string Title { get; set; } = "My Review";

    public string Summary { get; set; } =
        "A strong game with a few rough edges.";

    public ReviewRecommendation Recommendation { get; set; } =
        ReviewRecommendation.Recommended;
}

public enum ReviewRecommendation
{
    Recommended,
    Mixed,
    NotRecommended
}