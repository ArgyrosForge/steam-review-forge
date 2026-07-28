namespace SteamReviewForge.Models;

public enum ReviewValidationSection
{
    Setup,
    Format,
    Questions
}

public enum ReviewValidationSeverity
{
    Warning,
    Error
}

public sealed record ReviewValidationIssue(
    ReviewValidationSection Section,
    string Field,
    string Message,
    ReviewValidationSeverity Severity);