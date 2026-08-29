namespace SteamReviewForge.Models;

public sealed class ReviewDraft
{
    public ReviewEditingMode EditingMode { get; set; } =
        ReviewEditingMode.GuidedStructured;

    public string RawBbCode { get; set; } = string.Empty;

    public string Title { get; set; } = "[Game Title] Review";

    public string Summary { get; set; } = string.Empty;

    public ReviewRecommendation? Recommendation { get; set; }

    public bool ReceivedProductForFree { get; set; } = false;

    public ReviewDisplayFormat DisplayFormat { get; set; } =
        ReviewDisplayFormat.RatingTable;

    public ReviewRatingSystem RatingSystem { get; set; } =
        ReviewRatingSystem.FiveStars;

    public List<ReviewTableColumn> TableColumns { get; set; } =
    [
        new()
        {
            Heading = "Category",
            Kind = ReviewTableColumnKind.Category
        },
        new()
        {
            Heading = "Rating",
            Kind = ReviewTableColumnKind.Rating
        },
        new()
        {
            Heading = "Notes",
            Kind = ReviewTableColumnKind.Note
        }
    ];

    public List<string> TextRatingOptions { get; set; } =
    [
        "Terrible",
        "Bad",
        "Mixed",
        "Good",
        "Excellent"
    ];

    public ReviewTemplate Template { get; set; } =
        ReviewTemplate.Balanced;

    public string Playtime { get; set; } = string.Empty;

    public string WhatWorks { get; set; } =
        "Satisfying core gameplay\n" +
        "Lots of content to discover\n" +
        "Strong visual identity";

    public string WhatCouldBeBetter { get; set; } =
        "Occasional performance issues\n" +
        "Some systems need clearer explanations";

    public string FinalThoughts { get; set; } =
        "Despite its rough edges, the game delivers a memorable and consistently enjoyable experience.";

    public List<ReviewContentComponent> Components { get; set; } = [];

    public List<ReviewCategory> Categories { get; set; } = new()
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
    Recommended = 0,

    // Value 1 was the legacy Mixed option.
    NotRecommended = 2
}
