namespace SteamReviewForge.Models;

public sealed class ReviewTableColumn
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Heading { get; set; } = "New Column";

    public ReviewTableColumnKind Kind { get; set; } =
        ReviewTableColumnKind.CustomText;
}

public enum ReviewTableColumnKind
{
    Category,
    Rating,
    Note,
    CustomText
}
