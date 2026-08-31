namespace SteamReviewForge.Models;

public sealed class ReviewContentComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ReviewContentComponentKind Kind { get; set; }

    public string Heading { get; set; } = "New Section";

    public string Content { get; set; } = string.Empty;

    public ReviewTextFormat ContentFormat { get; set; } =
        ReviewTextFormat.Text;

    public int Rating { get; set; } = 3;
}

public enum ReviewContentComponentKind
{
    Rating = 0,
    BulletedList = 1,
    Text = 2
}
