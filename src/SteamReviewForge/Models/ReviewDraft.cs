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
    
    public ReviewTemplate Template { get; set; } =
        ReviewTemplate.Balanced;
    
    public string Playtime { get; set; } = "35 hours";

    public string WhatWorks { get; set; } =
        "Satisfying core gameplay\n" +
        "Lots of content to discover\n" +
        "Strong visual identity";

    public string WhatCouldBeBetter { get; set; } =
        "Occasional performance issues\n" +
        "Some systems need clearer explanations";

    public string FinalThoughts { get; set; } =
        "Despite its rough edges, the game delivers a memorable and consistently enjoyable experience.";
    
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