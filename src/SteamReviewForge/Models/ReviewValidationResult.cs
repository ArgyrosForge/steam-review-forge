namespace SteamReviewForge.Models;

public sealed class ReviewValidationResult
{
    public List<ReviewValidationIssue> Issues { get; } = [];

    public bool IsValid =>
        Issues.All(issue =>
            issue.Severity !=
            ReviewValidationSeverity.Error);

    public bool HasErrors(
        ReviewValidationSection section)
    {
        return Issues.Any(issue =>
            issue.Section == section &&
            issue.Severity ==
            ReviewValidationSeverity.Error);
    }

    public bool HasWarnings(
        ReviewValidationSection section)
    {
        return Issues.Any(issue =>
            issue.Section == section &&
            issue.Severity ==
            ReviewValidationSeverity.Warning);
    }

    public IEnumerable<ReviewValidationIssue> ForField(
        string field)
    {
        return Issues.Where(issue =>
            string.Equals(
                issue.Field,
                field,
                StringComparison.Ordinal));
    }
}