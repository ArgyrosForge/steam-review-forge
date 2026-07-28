namespace SteamReviewForge.Models;

public sealed class ReviewCategory
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; set; } = "New Category";

    public int Rating { get; set; } = 3;

    public string Note { get; set; } = string.Empty;
}
