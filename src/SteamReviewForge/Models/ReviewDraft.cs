namespace SteamReviewForge.Models;

public sealed class ReviewDraft
{
    public string Title { get; set; } = "My Review";

    public string Summary { get; set; } =
        "A strong game with a few rough edges.";

    public ReviewRecommendation Recommendation { get; set; } =
        ReviewRecommendation.Recommended;

    public ReviewDisplayFormat DisplayFormat { get; set; } =
        ReviewDisplayFormat.RatingTable;
    
    public List<ReviewCategory> Categories { get; } = new()
    {
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
        }
    };
}

public enum ReviewRecommendation
{
    Recommended,
    Mixed,
    NotRecommended
}